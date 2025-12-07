--------------------------------------------------------------------------------
-- GROUP_MANAGEMENT.SQL - Quản lý nhóm và cuộc trò chuyện
-- Bao gồm: Xóa nhóm, rời nhóm, xóa chat riêng tư (một phía), archive
--------------------------------------------------------------------------------

-- ============================================================================
-- 1) CẬP NHẬT BẢNG CUOCTROCHUYEN - Thêm cột IS_ARCHIVED
-- ============================================================================
-- Chạy lệnh này nếu cột chưa tồn tại
BEGIN
  EXECUTE IMMEDIATE 'ALTER TABLE CUOCTROCHUYEN ADD IS_ARCHIVED NUMBER(1) DEFAULT 0';
EXCEPTION WHEN OTHERS THEN
  IF SQLCODE = -1430 THEN NULL; -- Column already exists
  ELSE RAISE;
  END IF;
END;
/

-- Thêm cột ARCHIVED_AT để biết thời điểm archive
BEGIN
  EXECUTE IMMEDIATE 'ALTER TABLE CUOCTROCHUYEN ADD ARCHIVED_AT TIMESTAMP';
EXCEPTION WHEN OTHERS THEN
  IF SQLCODE = -1430 THEN NULL;
  ELSE RAISE;
  END IF;
END;
/

-- ============================================================================
-- 2) STORED PROCEDURES
-- ============================================================================

-- 2.1) Xóa chat riêng tư (một phía) - chỉ đánh dấu DELETED_BY_MEMBER
-- Khi người còn lại gửi tin nhắn mới, bên kia nhận được nhưng không thấy tin cũ
CREATE OR REPLACE PROCEDURE SP_XOA_CHAT_RIENGTU_MOTPHIA(
  p_mactc VARCHAR2,
  p_matk VARCHAR2
) AS
  v_is_private VARCHAR2(1);
BEGIN
  -- Kiểm tra có phải chat riêng tư không
  SELECT IS_PRIVATE INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
  
  IF v_is_private != 'Y' THEN
    RAISE_APPLICATION_ERROR(-20100, 'Chỉ có thể xóa một phía với chat riêng tư.');
  END IF;
  
  -- Đánh dấu DELETED_BY_MEMBER = 1 cho thành viên này
  UPDATE THANHVIEN 
  SET DELETED_BY_MEMBER = 1
  WHERE MACTC = p_mactc AND MATK = p_matk;
  
  -- Ghi audit log
  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
  VALUES(p_matk, 'DELETE_PRIVATE_CHAT_ONESIDE', p_mactc, 0);
  
  COMMIT;
END;
/

-- 2.2) Khôi phục chat riêng tư khi nhận tin nhắn mới
-- Được gọi khi người còn lại gửi tin nhắn
CREATE OR REPLACE PROCEDURE SP_KHOIPHUC_CHAT_RIENGTU(
  p_mactc VARCHAR2,
  p_matk VARCHAR2
) AS
BEGIN
  UPDATE THANHVIEN 
  SET DELETED_BY_MEMBER = 0
  WHERE MACTC = p_mactc AND MATK = p_matk;
  COMMIT;
END;
/

-- 2.3) Rời nhóm (cho member, không phải owner)
CREATE OR REPLACE PROCEDURE SP_ROI_NHOM(
  p_mactc VARCHAR2,
  p_matk VARCHAR2
) AS
  v_quyen VARCHAR2(100);
  v_is_private VARCHAR2(1);
BEGIN
  -- Kiểm tra không phải chat riêng tư
  SELECT IS_PRIVATE INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
  IF v_is_private = 'Y' THEN
    RAISE_APPLICATION_ERROR(-20101, 'Không thể rời chat riêng tư. Hãy sử dụng chức năng xóa cuộc trò chuyện.');
  END IF;
  
  -- Kiểm tra quyền - owner không thể rời nhóm
  SELECT QUYEN INTO v_quyen FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
  IF v_quyen = 'owner' THEN
    RAISE_APPLICATION_ERROR(-20102, 'Chủ nhóm không thể rời nhóm. Hãy chuyển quyền chủ nhóm hoặc xóa nhóm.');
  END IF;
  
  -- Xóa thành viên khỏi nhóm
  DELETE FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
  
  -- Ghi audit log
  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
  VALUES(p_matk, 'LEAVE_GROUP', p_mactc, 0);
  
  COMMIT;
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(-20103, 'Bạn không phải là thành viên của nhóm này.');
END;
/

-- 2.4) Xóa/Archive nhóm (chỉ owner)
-- Khi xóa nhóm: tất cả thành viên không thể nhắn tin, nhóm chuyển sang archive
CREATE OR REPLACE PROCEDURE SP_XOA_NHOM(
  p_mactc VARCHAR2,
  p_matk VARCHAR2  -- Người thực hiện (phải là owner)
) AS
  v_nguoiql VARCHAR2(20);
  v_quyen VARCHAR2(100);
  v_is_private VARCHAR2(1);
BEGIN
  -- Kiểm tra không phải chat riêng tư
  SELECT IS_PRIVATE INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
  IF v_is_private = 'Y' THEN
    RAISE_APPLICATION_ERROR(-20104, 'Không thể xóa chat riêng tư bằng chức năng này. Hãy sử dụng xóa cuộc trò chuyện.');
  END IF;
  
  -- Kiểm tra người thực hiện có phải owner không
  SELECT QUYEN INTO v_quyen FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
  IF v_quyen != 'owner' THEN
    RAISE_APPLICATION_ERROR(-20105, 'Chỉ chủ nhóm mới có thể xóa nhóm.');
  END IF;
  
  -- Archive nhóm thay vì xóa hoàn toàn
  UPDATE CUOCTROCHUYEN 
  SET IS_ARCHIVED = 1, ARCHIVED_AT = SYSTIMESTAMP
  WHERE MACTC = p_mactc;
  
  -- Ghi audit log
  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
  VALUES(p_matk, 'ARCHIVE_GROUP', p_mactc, 0);
  
  COMMIT;
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(-20106, 'Nhóm không tồn tại hoặc bạn không phải thành viên.');
END;
/

-- 2.5) Xóa archive (member tự xóa khỏi archived group)
CREATE OR REPLACE PROCEDURE SP_XOA_ARCHIVE(
  p_mactc VARCHAR2,
  p_matk VARCHAR2
) AS
  v_is_archived NUMBER;
BEGIN
  -- Kiểm tra nhóm đã archived chưa
  SELECT IS_ARCHIVED INTO v_is_archived FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
  IF v_is_archived != 1 THEN
    RAISE_APPLICATION_ERROR(-20107, 'Nhóm chưa được archive.');
  END IF;
  
  -- Xóa thành viên khỏi archived group
  DELETE FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
  
  -- Ghi audit log
  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
  VALUES(p_matk, 'DELETE_ARCHIVE', p_mactc, 0);
  
  COMMIT;
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(-20108, 'Nhóm không tồn tại.');
END;
/

-- 2.6) Kiểm tra trạng thái cuộc trò chuyện
CREATE OR REPLACE FUNCTION FN_KIEMTRA_TRANGTHAI_CTC(
  p_mactc VARCHAR2,
  p_matk VARCHAR2
) RETURN VARCHAR2 AS
  v_is_archived NUMBER;
  v_is_private VARCHAR2(1);
  v_deleted_by_member NUMBER;
BEGIN
  SELECT c.IS_ARCHIVED, c.IS_PRIVATE, NVL(tv.DELETED_BY_MEMBER, 0)
  INTO v_is_archived, v_is_private, v_deleted_by_member
  FROM CUOCTROCHUYEN c
  LEFT JOIN THANHVIEN tv ON c.MACTC = tv.MACTC AND tv.MATK = p_matk
  WHERE c.MACTC = p_mactc;
  
  IF v_is_archived = 1 THEN
    RETURN 'ARCHIVED';
  ELSIF v_is_private = 'Y' AND v_deleted_by_member = 1 THEN
    RETURN 'DELETED_BY_ME';
  ELSE
    RETURN 'ACTIVE';
  END IF;
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RETURN 'NOT_FOUND';
END;
/

-- ============================================================================
-- 3) CẬP NHẬT SP_GUI_TINNHAN để kiểm tra trạng thái và khôi phục chat
-- ============================================================================
CREATE OR REPLACE PROCEDURE SP_GUI_TINNHAN(
  p_mactc VARCHAR2,
  p_matk VARCHAR2,
  p_noidung CLOB,
  p_securitylabel NUMBER,
  p_matn OUT NUMBER,
  p_is_encrypted NUMBER DEFAULT 0,
  p_encrypted_key CLOB DEFAULT NULL,
  p_encryption_iv VARCHAR2 DEFAULT NULL,
  p_signature CLOB DEFAULT NULL
) AS
  v_is_archived NUMBER;
  v_is_private VARCHAR2(1);
  v_is_muted NUMBER;
BEGIN
  -- Set MAC context
  SET_MAC_CONTEXT(p_matk);
  
  -- Kiểm tra nhóm có bị archive không
  SELECT NVL(IS_ARCHIVED, 0), NVL(IS_PRIVATE, 'N') 
  INTO v_is_archived, v_is_private 
  FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
  
  IF v_is_archived = 1 THEN
    RAISE_APPLICATION_ERROR(-20110, 'Nhóm đã được archive. Không thể gửi tin nhắn.');
  END IF;
  
  -- Kiểm tra có bị mute không
  SELECT NVL(IS_MUTED, 0) INTO v_is_muted 
  FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
  
  IF v_is_muted = 1 THEN
    RAISE_APPLICATION_ERROR(-20111, 'Bạn đã bị tắt tiếng trong cuộc trò chuyện này.');
  END IF;
  
  -- Nếu là chat riêng tư, khôi phục cho tất cả thành viên đã xóa
  IF v_is_private = 'Y' THEN
    UPDATE THANHVIEN 
    SET DELETED_BY_MEMBER = 0 
    WHERE MACTC = p_mactc AND DELETED_BY_MEMBER = 1;
  END IF;
  
  INSERT INTO TINNHAN(MACTC, MATK, NOIDUNG, SECURITYLABEL, MALOAITN, MATRANGTHAI,
                      IS_ENCRYPTED, ENCRYPTED_KEY, ENCRYPTION_IV, SIGNATURE)
  VALUES(p_mactc, p_matk, p_noidung, p_securitylabel, 'TEXT', 'ACTIVE',
         p_is_encrypted, p_encrypted_key, p_encryption_iv, p_signature)
  RETURNING MATN INTO p_matn;
  
  COMMIT;
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(-20112, 'Cuộc trò chuyện không tồn tại hoặc bạn không phải thành viên.');
END;
/

-- ============================================================================
-- 4) CẬP NHẬT QUERY LẤY TIN NHẮN - Không lấy tin cũ nếu đã xóa một phía
-- ============================================================================
CREATE OR REPLACE PROCEDURE SP_LAY_TINNHAN_CUOCTROCHUYEN_V2(
  p_mactc IN VARCHAR2,
  p_matk IN VARCHAR2,
  p_limit IN NUMBER DEFAULT 100,
  p_cursor OUT SYS_REFCURSOR
) AS
  v_deleted_at TIMESTAMP;
  v_deleted_by_member NUMBER;
BEGIN
  -- Set MAC context trước khi query
  SET_MAC_CONTEXT(p_matk);
  
  -- Kiểm tra thời điểm xóa cuộc trò chuyện của user này
  BEGIN
    SELECT NGAYTHAMGIA, NVL(DELETED_BY_MEMBER, 0) 
    INTO v_deleted_at, v_deleted_by_member
    FROM THANHVIEN 
    WHERE MACTC = p_mactc AND MATK = p_matk;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      v_deleted_at := NULL;
      v_deleted_by_member := 0;
  END;
  
  -- Nếu user đã từng xóa cuộc trò chuyện và được khôi phục, 
  -- chỉ hiển thị tin nhắn sau thời điểm tham gia lại
  -- (Trong trường hợp này, NGAYTHAMGIA sẽ được cập nhật khi khôi phục)
  
  OPEN p_cursor FOR
    SELECT * FROM (
      SELECT 
        TN.MATN,
        TN.MACTC,
        TN.MATK,
        TK.TENTK AS SENDER_USERNAME,
        TN.NOIDUNG,
        TN.SECURITYLABEL,
        TN.NGAYGUI,
        TN.MALOAITN,
        TN.MATRANGTHAI,
        TN.IS_ENCRYPTED,
        TN.ENCRYPTED_KEY,
        TN.ENCRYPTION_IV,
        TN.SIGNATURE
      FROM TINNHAN TN
      JOIN TAIKHOAN TK ON TN.MATK = TK.MATK
      WHERE TN.MACTC = p_mactc
      ORDER BY TN.NGAYGUI DESC
    ) WHERE ROWNUM <= p_limit
    ORDER BY NGAYGUI ASC;
END;
/

COMMIT;

--------------------------------------------------------------------------------
-- HƯỚNG DẪN SỬ DỤNG:
-- 
-- 1. XÓA CHAT RIÊNG TƯ (MỘT PHÍA):
--    EXEC SP_XOA_CHAT_RIENGTU_MOTPHIA('CTC_P_001', 'TK001');
--    - Bên TK001 không còn thấy cuộc trò chuyện
--    - Bên còn lại vẫn thấy bình thường
--    - Khi bên còn lại gửi tin nhắn mới, TK001 sẽ nhận được nhưng không thấy tin cũ
--
-- 2. RỜI NHÓM:
--    EXEC SP_ROI_NHOM('CTC_IT_001', 'TK007');
--    - Member TK007 rời khỏi nhóm IT
--    - Owner không thể rời nhóm
--
-- 3. XÓA NHÓM (CHỈ OWNER):
--    EXEC SP_XOA_NHOM('CTC_IT_001', 'TK003');
--    - Nhóm được archive, không ai có thể nhắn tin
--    - Các thành viên vẫn có thể xem tin nhắn cũ
--
-- 4. XÓA ARCHIVE:
--    EXEC SP_XOA_ARCHIVE('CTC_IT_001', 'TK007');
--    - Member TK007 xóa nhóm đã archive khỏi danh sách của mình
--------------------------------------------------------------------------------
