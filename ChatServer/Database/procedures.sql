--------------------------------------------------------------------------------
-- PROCEDURES.SQL - STORED PROCEDURES CHO CHAT APPLICATION
-- Bao gồm: Các procedure xử lý nghiệp vụ, quản lý session, MAC Context
-- 
-- HƯỚNG DẪN THỰC THI: Chạy với user ChatApplication
--------------------------------------------------------------------------------

-- ============================================================================
-- 1) PACKAGE QUẢN LÝ MAC CONTEXT (Mandatory Access Control)
-- ============================================================================

CREATE OR REPLACE PACKAGE MAC_CTX_PKG AS
    -- Thiết lập mức bảo mật cho người dùng trong session
    PROCEDURE SET_USER_LEVEL(p_user IN VARCHAR2, p_level IN NUMBER);
    -- Xóa context hiện tại
    PROCEDURE CLEAR_CONTEXT;
    -- Lấy mức bảo mật của người dùng hiện tại
    FUNCTION GET_USER_LEVEL RETURN NUMBER;
    -- Lấy username của người dùng hiện tại
    FUNCTION GET_USERNAME RETURN VARCHAR2;
    -- Kiểm tra quyền đọc (no-read-up)
    FUNCTION CAN_READ(p_label IN NUMBER) RETURN BOOLEAN;
    -- Kiểm tra quyền ghi (no-write-up)
    FUNCTION CAN_WRITE(p_label IN NUMBER) RETURN BOOLEAN;
END MAC_CTX_PKG;
/

CREATE OR REPLACE PACKAGE BODY MAC_CTX_PKG AS
    PROCEDURE SET_USER_LEVEL(p_user IN VARCHAR2, p_level IN NUMBER) IS
    BEGIN
        DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'USERNAME', p_user);
        DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'USER_LEVEL', TO_CHAR(p_level));
        DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'LOGIN_TIME', TO_CHAR(SYSTIMESTAMP, 'YYYY-MM-DD HH24:MI:SS'));
    END;
    
    PROCEDURE CLEAR_CONTEXT IS
    BEGIN
        DBMS_SESSION.CLEAR_CONTEXT('MAC_CTX');
    END;
    
    FUNCTION GET_USER_LEVEL RETURN NUMBER IS
    BEGIN
        RETURN TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    END;
    
    FUNCTION GET_USERNAME RETURN VARCHAR2 IS
    BEGIN
        RETURN NVL(SYS_CONTEXT('MAC_CTX', 'USERNAME'), USER);
    END;
    
    FUNCTION CAN_READ(p_label IN NUMBER) RETURN BOOLEAN IS
        v_user_level NUMBER := GET_USER_LEVEL;
    BEGIN
        -- No-read-up: Người dùng chỉ đọc được dữ liệu có label <= level của họ
        RETURN p_label <= v_user_level;
    END;
    
    FUNCTION CAN_WRITE(p_label IN NUMBER) RETURN BOOLEAN IS
        v_user_level NUMBER := GET_USER_LEVEL;
    BEGIN
        -- No-write-up: Người dùng chỉ ghi được dữ liệu có label <= level của họ
        RETURN p_label <= v_user_level;
    END;
END MAC_CTX_PKG;
/

-- ============================================================================
-- 2) PACKAGE QUẢN LÝ SESSION
-- ============================================================================

CREATE OR REPLACE PACKAGE SESSION_CTX_PKG AS
    PROCEDURE SET_SESSION_INFO(p_session_id IN VARCHAR2, p_matk IN VARCHAR2, p_ip IN VARCHAR2);
    PROCEDURE CLEAR_SESSION;
    FUNCTION GET_SESSION_ID RETURN VARCHAR2;
    FUNCTION GET_CURRENT_USER RETURN VARCHAR2;
    FUNCTION GET_CLIENT_IP RETURN VARCHAR2;
END SESSION_CTX_PKG;
/

CREATE OR REPLACE PACKAGE BODY SESSION_CTX_PKG AS
    PROCEDURE SET_SESSION_INFO(p_session_id IN VARCHAR2, p_matk IN VARCHAR2, p_ip IN VARCHAR2) IS
    BEGIN
        DBMS_SESSION.SET_CONTEXT('SESSION_CTX', 'SESSION_ID', p_session_id);
        DBMS_SESSION.SET_CONTEXT('SESSION_CTX', 'MATK', p_matk);
        DBMS_SESSION.SET_CONTEXT('SESSION_CTX', 'CLIENT_IP', p_ip);
        DBMS_SESSION.SET_CONTEXT('SESSION_CTX', 'LOGIN_TIME', TO_CHAR(SYSTIMESTAMP, 'YYYY-MM-DD HH24:MI:SS'));
    END;
    
    PROCEDURE CLEAR_SESSION IS
    BEGIN
        DBMS_SESSION.CLEAR_CONTEXT('SESSION_CTX');
    END;
    
    FUNCTION GET_SESSION_ID RETURN VARCHAR2 IS
    BEGIN
        RETURN SYS_CONTEXT('SESSION_CTX', 'SESSION_ID');
    END;
    
    FUNCTION GET_CURRENT_USER RETURN VARCHAR2 IS
    BEGIN
        RETURN SYS_CONTEXT('SESSION_CTX', 'MATK');
    END;
    
    FUNCTION GET_CLIENT_IP RETURN VARCHAR2 IS
    BEGIN
        RETURN SYS_CONTEXT('SESSION_CTX', 'CLIENT_IP');
    END;
END SESSION_CTX_PKG;
/

-- ============================================================================
-- 3) PACKAGE QUẢN LÝ ADMIN CONTEXT
-- ============================================================================

CREATE OR REPLACE PACKAGE ADMIN_CTX_PKG AS
    PROCEDURE SET_ADMIN_MODE(p_matk IN VARCHAR2, p_is_admin IN NUMBER);
    FUNCTION IS_ADMIN RETURN BOOLEAN;
    FUNCTION GET_ADMIN_USER RETURN VARCHAR2;
END ADMIN_CTX_PKG;
/

CREATE OR REPLACE PACKAGE BODY ADMIN_CTX_PKG AS
    PROCEDURE SET_ADMIN_MODE(p_matk IN VARCHAR2, p_is_admin IN NUMBER) IS
    BEGIN
        DBMS_SESSION.SET_CONTEXT('ADMIN_CTX', 'MATK', p_matk);
        DBMS_SESSION.SET_CONTEXT('ADMIN_CTX', 'IS_ADMIN', TO_CHAR(p_is_admin));
    END;
    
    FUNCTION IS_ADMIN RETURN BOOLEAN IS
    BEGIN
        RETURN NVL(SYS_CONTEXT('ADMIN_CTX', 'IS_ADMIN'), '0') = '1';
    END;
    
    FUNCTION GET_ADMIN_USER RETURN VARCHAR2 IS
    BEGIN
        RETURN SYS_CONTEXT('ADMIN_CTX', 'MATK');
    END;
END ADMIN_CTX_PKG;
/

-- ============================================================================
-- 4) PROCEDURE THIẾT LẬP MAC CONTEXT
-- ============================================================================

CREATE OR REPLACE PROCEDURE SET_MAC_CONTEXT(
    p_matk IN VARCHAR2,
    p_level IN NUMBER DEFAULT NULL
) AS
    v_level NUMBER;
BEGIN
    IF p_level IS NULL THEN
        BEGIN
            SELECT CLEARANCELEVEL INTO v_level FROM TAIKHOAN WHERE MATK = p_matk;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN v_level := 1;
        END;
    ELSE
        v_level := p_level;
    END IF;
    
    MAC_CTX_PKG.SET_USER_LEVEL(p_matk, v_level);
EXCEPTION
    WHEN OTHERS THEN
        MAC_CTX_PKG.SET_USER_LEVEL(p_matk, 1);
END;
/

-- ============================================================================
-- 5) PROCEDURES QUẢN LÝ TÀI KHOẢN
-- ============================================================================

-- Tạo tài khoản mới
CREATE OR REPLACE PROCEDURE SP_TAO_TAIKHOAN(
    p_matk VARCHAR2,
    p_tentk VARCHAR2,
    p_password_hash VARCHAR2,
    p_mavaitro VARCHAR2,
    p_clearance NUMBER,
    p_public_key CLOB DEFAULT NULL
) AS
BEGIN
    INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, PUBLIC_KEY)
    VALUES(p_matk, p_tentk, p_password_hash, p_mavaitro, p_clearance, p_public_key);
    
    -- Ghi audit log
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL, DETAILS)
    VALUES('SYSTEM', 'CREATE_ACCOUNT', p_matk, 0, 'Tạo tài khoản: ' || p_tentk);
    
    COMMIT;
END;
/

-- Đổi mật khẩu
CREATE OR REPLACE PROCEDURE SP_DOI_MATKHAU(
    p_matk VARCHAR2,
    p_new_password_hash VARCHAR2
) AS
BEGIN
    UPDATE TAIKHOAN SET PASSWORD_HASH = p_new_password_hash WHERE MATK = p_matk;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk, 'CHANGE_PASSWORD', p_matk, 0);
    
    COMMIT;
END;
/

-- Cập nhật thông tin cá nhân
CREATE OR REPLACE PROCEDURE SP_CAPNHAT_THONGTIN_CANHAN(
    p_matk VARCHAR2,
    p_hovaten VARCHAR2,
    p_email VARCHAR2,
    p_sdt VARCHAR2,
    p_ngaysinh DATE,
    p_mapb VARCHAR2 DEFAULT NULL,
    p_macv VARCHAR2 DEFAULT NULL,
    p_diachi VARCHAR2 DEFAULT NULL,
    p_bio VARCHAR2 DEFAULT NULL
) AS
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM NGUOIDUNG WHERE MATK = p_matk;
    
    IF v_count = 0 THEN
        INSERT INTO NGUOIDUNG(MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO, NGAYCAPNHAT)
        VALUES(p_matk, p_mapb, p_macv, p_hovaten, p_email, p_sdt, p_ngaysinh, p_diachi, p_bio, SYSTIMESTAMP);
    ELSE
        UPDATE NGUOIDUNG 
        SET HOVATEN = p_hovaten, 
            EMAIL = p_email, 
            SDT = p_sdt, 
            NGAYSINH = p_ngaysinh,
            MAPB = NVL(p_mapb, MAPB),
            MACV = NVL(p_macv, MACV),
            DIACHI = NVL(p_diachi, DIACHI),
            BIO = NVL(p_bio, BIO),
            NGAYCAPNHAT = SYSTIMESTAMP
        WHERE MATK = p_matk;
    END IF;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk, 'UPDATE_PROFILE', p_matk, 0);
    
    COMMIT;
END;
/

-- Cập nhật thông tin người dùng (Admin)
CREATE OR REPLACE PROCEDURE SP_CAPNHAT_NGUOIDUNG_ADMIN(
    p_matk VARCHAR2,
    p_email VARCHAR2 DEFAULT NULL,
    p_hovaten VARCHAR2 DEFAULT NULL,
    p_sdt VARCHAR2 DEFAULT NULL,
    p_clearance NUMBER DEFAULT NULL,
    p_mavaitro VARCHAR2 DEFAULT NULL
) AS
    v_old_clearance NUMBER;
    v_old_mavaitro VARCHAR2(20);
BEGIN
    -- Lưu giá trị cũ để audit
    SELECT CLEARANCELEVEL, MAVAITRO INTO v_old_clearance, v_old_mavaitro
    FROM TAIKHOAN WHERE MATK = p_matk;
    
    IF p_email IS NOT NULL OR p_hovaten IS NOT NULL OR p_sdt IS NOT NULL THEN
        MERGE INTO NGUOIDUNG n
        USING (SELECT p_matk AS MATK FROM DUAL) t
        ON (n.MATK = t.MATK)
        WHEN MATCHED THEN
            UPDATE SET 
                EMAIL = NVL(p_email, n.EMAIL),
                HOVATEN = NVL(p_hovaten, n.HOVATEN),
                SDT = NVL(p_sdt, n.SDT),
                NGAYCAPNHAT = SYSTIMESTAMP
        WHEN NOT MATCHED THEN
            INSERT (MATK, EMAIL, HOVATEN, SDT)
            VALUES (p_matk, p_email, p_hovaten, p_sdt);
    END IF;
    
    IF p_clearance IS NOT NULL OR p_mavaitro IS NOT NULL THEN
        UPDATE TAIKHOAN 
        SET CLEARANCELEVEL = NVL(p_clearance, CLEARANCELEVEL),
            MAVAITRO = NVL(p_mavaitro, MAVAITRO)
        WHERE MATK = p_matk;
    END IF;
    
    -- Ghi audit log với chi tiết thay đổi
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL, OLD_VALUE, NEW_VALUE)
    VALUES(
        NVL(ADMIN_CTX_PKG.GET_ADMIN_USER, 'ADMIN'), 
        'ADMIN_UPDATE_USER', 
        p_matk, 
        0,
        'Clearance: ' || v_old_clearance || ', Role: ' || v_old_mavaitro,
        'Clearance: ' || NVL(TO_CHAR(p_clearance), TO_CHAR(v_old_clearance)) || 
        ', Role: ' || NVL(p_mavaitro, v_old_mavaitro)
    );
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20001, 'Không tìm thấy tài khoản: ' || p_matk);
END;
/

-- Ban/Unban user global
CREATE OR REPLACE PROCEDURE SP_BAN_USER_GLOBAL(p_matk VARCHAR2) AS
BEGIN
    UPDATE TAIKHOAN SET IS_BANNED_GLOBAL = 1 WHERE MATK = p_matk;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'BAN_USER_GLOBAL', p_matk, 0);
    
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_UNBAN_USER_GLOBAL(p_matk VARCHAR2) AS
BEGIN
    UPDATE TAIKHOAN SET IS_BANNED_GLOBAL = 0 WHERE MATK = p_matk;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'UNBAN_USER_GLOBAL', p_matk, 0);
    
    COMMIT;
END;
/

-- Xóa tài khoản hoàn toàn
CREATE OR REPLACE PROCEDURE SP_XOA_TAIKHOAN(p_matk VARCHAR2) AS
BEGIN
    DELETE FROM AUDIT_LOGS WHERE MATK = p_matk;
    DELETE FROM TAIKHOAN WHERE MATK = p_matk;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES('ADMIN', 'DELETE_ACCOUNT', p_matk, 0);
    
    COMMIT;
END;
/

-- ============================================================================
-- 6) PROCEDURES QUẢN LÝ PHIÊN ĐĂNG NHẬP
-- ============================================================================

-- Tạo phiên đăng nhập mới
CREATE OR REPLACE PROCEDURE SP_TAO_PHIEN(
    p_matk VARCHAR2,
    p_ip_address VARCHAR2 DEFAULT NULL,
    p_user_agent VARCHAR2 DEFAULT NULL,
    p_session_timeout_hours NUMBER DEFAULT 8,
    p_maphien OUT VARCHAR2
) AS
    v_session_id VARCHAR2(50);
    v_clearance NUMBER;
BEGIN
    -- Tạo session ID unique
    v_session_id := 'SES_' || p_matk || '_' || TO_CHAR(SYSTIMESTAMP, 'YYYYMMDDHH24MISSFF6');
    
    -- Lấy clearance level
    SELECT CLEARANCELEVEL INTO v_clearance FROM TAIKHOAN WHERE MATK = p_matk;
    
    -- Thêm phiên mới
    INSERT INTO PHIENDANGNHAP(MAPHIEN, MATK, IP_ADDRESS, USER_AGENT, THOIDIEM_HETHAN, CLEARANCELEVEL_SESSION)
    VALUES(v_session_id, p_matk, p_ip_address, p_user_agent, 
           SYSTIMESTAMP + NUMTODSINTERVAL(p_session_timeout_hours, 'HOUR'), v_clearance);
    
    -- Cập nhật thông tin đăng nhập
    UPDATE TAIKHOAN SET LAST_LOGIN = SYSTIMESTAMP, LOGIN_COUNT = LOGIN_COUNT + 1 WHERE MATK = p_matk;
    
    -- Set session context
    SESSION_CTX_PKG.SET_SESSION_INFO(v_session_id, p_matk, p_ip_address);
    SET_MAC_CONTEXT(p_matk, v_clearance);
    
    -- Ghi audit log
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SESSION_ID, IP_ADDRESS, USER_AGENT)
    VALUES(p_matk, 'LOGIN', 'SESSION', v_session_id, p_ip_address, p_user_agent);
    
    p_maphien := v_session_id;
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20002, 'Không tìm thấy tài khoản: ' || p_matk);
END;
/

-- Kết thúc phiên đăng nhập
CREATE OR REPLACE PROCEDURE SP_KET_THUC_PHIEN(p_maphien VARCHAR2) AS
    v_matk VARCHAR2(20);
BEGIN
    SELECT MATK INTO v_matk FROM PHIENDANGNHAP WHERE MAPHIEN = p_maphien;
    
    UPDATE PHIENDANGNHAP SET TRANG_THAI = 'LOGGED_OUT' WHERE MAPHIEN = p_maphien;
    UPDATE TAIKHOAN SET LAST_LOGOUT = SYSTIMESTAMP WHERE MATK = v_matk;
    
    -- Xóa context
    SESSION_CTX_PKG.CLEAR_SESSION;
    MAC_CTX_PKG.CLEAR_CONTEXT;
    
    -- Ghi audit log
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SESSION_ID)
    VALUES(v_matk, 'LOGOUT', 'SESSION', p_maphien);
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN NULL;
END;
/

-- Kiểm tra phiên hợp lệ
CREATE OR REPLACE FUNCTION FN_KIEM_TRA_PHIEN(p_maphien VARCHAR2) RETURN NUMBER AS
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM PHIENDANGNHAP
    WHERE MAPHIEN = p_maphien 
      AND TRANG_THAI = 'ACTIVE' 
      AND THOIDIEM_HETHAN > SYSTIMESTAMP;
    RETURN v_count;
END;
/

-- ============================================================================
-- 7) PROCEDURES QUẢN LÝ CUỘC TRÒ CHUYỆN
-- ============================================================================

-- Tạo cuộc trò chuyện
CREATE OR REPLACE PROCEDURE SP_TAO_CUOCTROCHUYEN(
    p_mactc VARCHAR2,
    p_maloaictc VARCHAR2,
    p_tenctc VARCHAR2,
    p_nguoiql VARCHAR2,
    p_is_private VARCHAR2,
    p_created_by VARCHAR2,
    p_is_encrypted NUMBER DEFAULT 0
) AS
    v_resolved_matk VARCHAR2(20);
BEGIN
    -- Resolve TENTK thành MATK nếu cần
    SELECT MATK INTO v_resolved_matk 
    FROM TAIKHOAN 
    WHERE MATK = p_created_by OR TENTK = p_created_by
    FETCH FIRST 1 ROW ONLY;
    
    INSERT INTO CUOCTROCHUYEN(MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, IS_ENCRYPTED)
    VALUES(p_mactc, p_maloaictc, p_tenctc, v_resolved_matk, p_is_private, v_resolved_matk, p_is_encrypted);
    
    INSERT INTO THANHVIEN(MACTC, MATK, QUYEN, MAPHANQUYEN)
    VALUES(p_mactc, v_resolved_matk, 'owner', 'OWNER');
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(v_resolved_matk, 'CREATE_CONVERSATION', p_mactc, 0);
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20001, 'Không tìm thấy người dùng: ' || p_created_by);
END;
/

-- Xóa cuộc trò chuyện
CREATE OR REPLACE PROCEDURE SP_XOA_CUOCTROCHUYEN(p_mactc VARCHAR2) AS
BEGIN
    DELETE FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'DELETE_CONVERSATION', p_mactc, 0);
    
    COMMIT;
END;
/

-- Thêm thành viên
CREATE OR REPLACE PROCEDURE SP_THEM_THANHVIEN(
    p_mactc VARCHAR2,
    p_matk VARCHAR2,
    p_quyen VARCHAR2 DEFAULT 'member',
    p_maphanquyen VARCHAR2 DEFAULT 'MEMBER'
) AS
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM THANHVIEN 
    WHERE MACTC = p_mactc AND MATK = p_matk AND DELETED_BY_MEMBER = 0;
    
    IF v_count = 0 THEN
        INSERT INTO THANHVIEN(MACTC, MATK, QUYEN, MAPHANQUYEN)
        VALUES(p_mactc, p_matk, p_quyen, p_maphanquyen);
        
        INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
        VALUES(MAC_CTX_PKG.GET_USERNAME, 'ADD_MEMBER', p_mactc || ':' || p_matk, 0);
        
        COMMIT;
    ELSE
        RAISE_APPLICATION_ERROR(-20080, 'Thành viên đã tồn tại trong cuộc trò chuyện.');
    END IF;
END;
/

-- Xóa thành viên
CREATE OR REPLACE PROCEDURE SP_XOA_THANHVIEN(
    p_mactc VARCHAR2,
    p_matk VARCHAR2
) AS
BEGIN
    DELETE FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'REMOVE_MEMBER', p_mactc || ':' || p_matk, 0);
    
    COMMIT;
END;
/

-- Thiết lập trưởng nhóm
CREATE OR REPLACE PROCEDURE SP_SET_TRUONGNHOM(
    p_mactc VARCHAR2,
    p_matk VARCHAR2
) AS
BEGIN
    -- Hạ quyền owner cũ
    UPDATE THANHVIEN SET QUYEN = 'admin', MAPHANQUYEN = 'ADMIN' 
    WHERE MACTC = p_mactc AND QUYEN = 'owner';
    
    -- Nâng quyền owner mới
    UPDATE THANHVIEN SET QUYEN = 'owner', MAPHANQUYEN = 'OWNER' 
    WHERE MACTC = p_mactc AND MATK = p_matk;
    
    -- Cập nhật người quản lý trong cuộc trò chuyện
    UPDATE CUOCTROCHUYEN SET NGUOIQL = p_matk WHERE MACTC = p_mactc;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'TRANSFER_OWNERSHIP', p_mactc || ':' || p_matk, 0);
    
    COMMIT;
END;
/

-- Ban/Unban/Mute/Unmute member trong nhóm
CREATE OR REPLACE PROCEDURE SP_BAN_MEMBER(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
    UPDATE THANHVIEN SET IS_BANNED = 1 WHERE MACTC = p_mactc AND MATK = p_matk;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'BAN_MEMBER', p_mactc || ':' || p_matk, 0);
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_UNBAN_MEMBER(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
    UPDATE THANHVIEN SET IS_BANNED = 0 WHERE MACTC = p_mactc AND MATK = p_matk;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'UNBAN_MEMBER', p_mactc || ':' || p_matk, 0);
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_MUTE_MEMBER(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
    UPDATE THANHVIEN SET IS_MUTED = 1 WHERE MACTC = p_mactc AND MATK = p_matk;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'MUTE_MEMBER', p_mactc || ':' || p_matk, 0);
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_UNMUTE_MEMBER(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
    UPDATE THANHVIEN SET IS_MUTED = 0 WHERE MACTC = p_mactc AND MATK = p_matk;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'UNMUTE_MEMBER', p_mactc || ':' || p_matk, 0);
    COMMIT;
END;
/

-- Rời nhóm
CREATE OR REPLACE PROCEDURE SP_ROI_NHOM(
    p_mactc VARCHAR2,
    p_matk VARCHAR2
) AS
    v_quyen VARCHAR2(100);
    v_is_private VARCHAR2(1);
BEGIN
    SELECT IS_PRIVATE INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
    IF v_is_private = 'Y' THEN
        RAISE_APPLICATION_ERROR(-20101, 'Không thể rời chat riêng tư.');
    END IF;
    
    SELECT QUYEN INTO v_quyen FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
    IF v_quyen = 'owner' THEN
        RAISE_APPLICATION_ERROR(-20102, 'Chủ nhóm không thể rời nhóm.');
    END IF;
    
    DELETE FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk, 'LEAVE_GROUP', p_mactc, 0);
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20103, 'Bạn không phải thành viên của nhóm.');
END;
/

-- ============================================================================
-- 8) PROCEDURES QUẢN LÝ TIN NHẮN
-- ============================================================================

-- Gửi tin nhắn (có kiểm tra MAC)
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
BEGIN
    -- Set MAC context
    SET_MAC_CONTEXT(p_matk);
    
    INSERT INTO TINNHAN(MACTC, MATK, NOIDUNG, SECURITYLABEL, MALOAITN, MATRANGTHAI,
                        IS_ENCRYPTED, ENCRYPTED_KEY, ENCRYPTION_IV, SIGNATURE)
    VALUES(p_mactc, p_matk, p_noidung, p_securitylabel, 'TEXT', 'ACTIVE',
           p_is_encrypted, p_encrypted_key, p_encryption_iv, p_signature)
    RETURNING MATN INTO p_matn;
    
    COMMIT;
END;
/

-- Gửi tin nhắn với attachment
CREATE OR REPLACE PROCEDURE SP_GUI_TINNHAN_WITH_ATTACH(
    p_mactc VARCHAR2,
    p_matk VARCHAR2,
    p_noidung CLOB,
    p_securitylabel NUMBER,
    p_attach_id NUMBER,
    p_matn OUT NUMBER
) AS
BEGIN
    SET_MAC_CONTEXT(p_matk);
    
    INSERT INTO TINNHAN(MACTC, MATK, NOIDUNG, SECURITYLABEL, MALOAITN, MATRANGTHAI)
    VALUES(p_mactc, p_matk, p_noidung, p_securitylabel, 'FILE', 'ACTIVE')
    RETURNING MATN INTO p_matn;
    
    INSERT INTO TINNHAN_ATTACH(MATN, ATTACH_ID) VALUES(p_matn, p_attach_id);
    
    COMMIT;
END;
/

-- Gửi tin nhắn riêng tư
CREATE OR REPLACE PROCEDURE SP_GUI_TINNHAN_RIENG(
    p_matk_sender VARCHAR2,
    p_matk_receiver VARCHAR2,
    p_noidung CLOB,
    p_securitylabel NUMBER,
    p_mactc OUT VARCHAR2,
    p_matn OUT NUMBER
) AS
    v_mactc VARCHAR2(60);
BEGIN
    SET_MAC_CONTEXT(p_matk_sender);
    
    -- Tìm cuộc trò chuyện riêng giữa 2 người
    BEGIN
        SELECT MACTC INTO v_mactc
        FROM (
            SELECT c.MACTC FROM CUOCTROCHUYEN c
            WHERE c.IS_PRIVATE = 'Y'
            AND EXISTS (SELECT 1 FROM THANHVIEN t1 WHERE t1.MACTC = c.MACTC AND t1.MATK = p_matk_sender AND t1.DELETED_BY_MEMBER = 0)
            AND EXISTS (SELECT 1 FROM THANHVIEN t2 WHERE t2.MACTC = c.MACTC AND t2.MATK = p_matk_receiver AND t2.DELETED_BY_MEMBER = 0)
            ORDER BY c.NGAYTAO DESC
        ) WHERE ROWNUM = 1;
        
        p_mactc := v_mactc;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_mactc := 'CTC_P_' || TO_CHAR(SYSTIMESTAMP, 'YYYYMMDDHH24MISSFF6');
            
            INSERT INTO CUOCTROCHUYEN(MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY)
            VALUES(v_mactc, 'PRIVATE', 'Chat Riêng Tư', p_matk_sender, 'Y', p_matk_sender);
            
            INSERT INTO THANHVIEN(MACTC, MATK, QUYEN, MAPHANQUYEN)
            VALUES(v_mactc, p_matk_sender, 'member', 'MEMBER');
            
            INSERT INTO THANHVIEN(MACTC, MATK, QUYEN, MAPHANQUYEN)
            VALUES(v_mactc, p_matk_receiver, 'member', 'MEMBER');
            
            p_mactc := v_mactc;
    END;
    
    INSERT INTO TINNHAN(MACTC, MATK, NOIDUNG, SECURITYLABEL, MALOAITN, MATRANGTHAI)
    VALUES(p_mactc, p_matk_sender, p_noidung, p_securitylabel, 'TEXT', 'ACTIVE')
    RETURNING MATN INTO p_matn;
    
    COMMIT;
END;
/

-- Xóa tin nhắn (Admin)
CREATE OR REPLACE PROCEDURE SP_XOA_TINNHAN(p_matn NUMBER) AS
BEGIN
    DELETE FROM TINNHAN WHERE MATN = p_matn;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'DELETE_MESSAGE', TO_CHAR(p_matn), 0);
    
    COMMIT;
END;
/

-- Lấy tin nhắn cuộc trò chuyện (có set context)
CREATE OR REPLACE PROCEDURE SP_LAY_TINNHAN_CUOCTROCHUYEN(
    p_mactc IN VARCHAR2,
    p_matk IN VARCHAR2,
    p_limit IN NUMBER DEFAULT 100,
    p_cursor OUT SYS_REFCURSOR
) AS
BEGIN
    -- Set MAC context trước khi query
    SET_MAC_CONTEXT(p_matk);
    
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

-- ============================================================================
-- 9) PROCEDURES QUẢN LÝ ATTACHMENT
-- ============================================================================

-- Upload attachment
CREATE OR REPLACE PROCEDURE SP_UPLOAD_ATTACHMENT(
    p_matk VARCHAR2,
    p_filename VARCHAR2,
    p_mimetype VARCHAR2,
    p_filesize NUMBER,
    p_filedata BLOB,
    p_attach_id OUT NUMBER,
    p_is_encrypted NUMBER DEFAULT 0
) AS
BEGIN
    INSERT INTO ATTACHMENT(MATK, FILENAME, MIMETYPE, FILESIZE, FILEDATA, IS_ENCRYPTED)
    VALUES(p_matk, p_filename, p_mimetype, p_filesize, p_filedata, p_is_encrypted)
    RETURNING ATTACH_ID INTO p_attach_id;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk, 'UPLOAD_ATTACHMENT', p_filename, 0);
    
    COMMIT;
END;
/

-- Upload attachment với mã hóa
CREATE OR REPLACE PROCEDURE SP_UPLOAD_ATTACHMENT_ENCRYPTED(
    p_matk VARCHAR2,
    p_filename VARCHAR2,
    p_mimetype VARCHAR2,
    p_filesize NUMBER,
    p_filedata BLOB,
    p_encryption_key VARCHAR2,
    p_encryption_iv VARCHAR2,
    p_attach_id OUT NUMBER
) AS
BEGIN
    INSERT INTO ATTACHMENT(MATK, FILENAME, MIMETYPE, FILESIZE, FILEDATA, ENCRYPTION_KEY, ENCRYPTION_IV, IS_ENCRYPTED)
    VALUES(p_matk, p_filename, p_mimetype, p_filesize, p_filedata, p_encryption_key, p_encryption_iv, 1)
    RETURNING ATTACH_ID INTO p_attach_id;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk, 'UPLOAD_ENCRYPTED_ATTACHMENT', p_filename, 0);
    
    COMMIT;
END;
/

-- ============================================================================
-- 10) PROCEDURES QUẢN LÝ OTP
-- ============================================================================

-- Tạo OTP
CREATE OR REPLACE PROCEDURE SP_TAO_OTP(
    p_matk VARCHAR2,
    p_email VARCHAR2,
    p_otp_hash VARCHAR2,
    p_expiry_minutes NUMBER DEFAULT 10,
    p_maotp OUT NUMBER
) AS
BEGIN
    INSERT INTO XACTHUCOTP (MATK, EMAIL, MAXTOTP, THOIGIANTONTAI)
    VALUES (p_matk, p_email, p_otp_hash, SYSTIMESTAMP + NUMTODSINTERVAL(p_expiry_minutes, 'MINUTE'))
    RETURNING MAOTP INTO p_maotp;
    COMMIT;
END;
/

-- ============================================================================
-- 11) PROCEDURES TRUY VẤN DỮ LIỆU
-- ============================================================================

-- Lấy chi tiết cuộc trò chuyện
CREATE OR REPLACE PROCEDURE SP_LAY_CHITIET_CUOCTROCHUYEN(
    p_mactc VARCHAR2,
    p_cursor OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_cursor FOR
        SELECT 
            c.MACTC, 
            c.TENCTC, 
            c.NGAYTAO, 
            c.IS_PRIVATE,
            c.NGUOIQL, 
            t_owner.TENTK AS TENTK_NGUOIQL,
            t_owner.CLEARANCELEVEL AS OWNER_CLEARANCE,
            lc.TENLOAICTC,
            lc.MOTA AS LOAI_MOTA,
            c.MOTA,
            c.IS_ENCRYPTED,
            c.AVATAR_URL,
            (SELECT COUNT(*) FROM THANHVIEN tv WHERE tv.MACTC = c.MACTC AND tv.DELETED_BY_MEMBER = 0) AS MEMBER_COUNT,
            (SELECT COUNT(*) FROM TINNHAN tn WHERE tn.MACTC = c.MACTC) AS MESSAGE_COUNT
        FROM CUOCTROCHUYEN c
        LEFT JOIN TAIKHOAN t_owner ON c.NGUOIQL = t_owner.MATK
        LEFT JOIN LOAICTC lc ON c.MALOAICTC = lc.MALOAICTC
        WHERE c.MACTC = p_mactc;
END;
/

-- Lấy danh sách thành viên chi tiết
CREATE OR REPLACE PROCEDURE SP_LAY_THANHVIEN_CHITIET(
    p_mactc VARCHAR2,
    p_cursor OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_cursor FOR
        SELECT 
            tv.MATK, 
            tk.TENTK, 
            tv.QUYEN, 
            tv.MAPHANQUYEN,
            tv.IS_BANNED, 
            tv.IS_MUTED, 
            tv.NGAYTHAMGIA,
            tv.NICKNAME,
            n.HOVATEN, 
            n.EMAIL,
            n.SDT,
            pb.TENPB AS PHONGBAN,
            cv.TENCV AS CHUCVU,
            tk.CLEARANCELEVEL
        FROM THANHVIEN tv
        JOIN TAIKHOAN tk ON tv.MATK = tk.MATK
        LEFT JOIN NGUOIDUNG n ON tv.MATK = n.MATK
        LEFT JOIN PHONGBAN pb ON n.MAPB = pb.MAPB
        LEFT JOIN CHUCVU cv ON n.MACV = cv.MACV
        WHERE tv.MACTC = p_mactc AND tv.DELETED_BY_MEMBER = 0
        ORDER BY 
            CASE tv.QUYEN 
                WHEN 'owner' THEN 1 
                WHEN 'admin' THEN 2 
                WHEN 'moderator' THEN 3 
                ELSE 4 
            END,
            tv.NGAYTHAMGIA;
END;
/

-- Lấy thông tin người dùng đầy đủ
CREATE OR REPLACE PROCEDURE SP_LAY_THONGTIN_NGUOIDUNG(
    p_matk VARCHAR2,
    p_cursor OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_cursor FOR
        SELECT 
            t.MATK, 
            t.TENTK, 
            t.CLEARANCELEVEL, 
            t.IS_BANNED_GLOBAL, 
            t.NGAYTAO,
            t.LAST_LOGIN,
            t.PUBLIC_KEY,
            n.HOVATEN, 
            n.EMAIL, 
            n.SDT, 
            n.NGAYSINH,
            n.DIACHI,
            n.BIO,
            n.AVATAR_URL,
            pb.MAPB,
            pb.TENPB AS PHONGBAN,
            cv.MACV,
            cv.TENCV AS CHUCVU,
            v.TENVAITRO AS VAITRO
        FROM TAIKHOAN t
        LEFT JOIN NGUOIDUNG n ON t.MATK = n.MATK
        LEFT JOIN PHONGBAN pb ON n.MAPB = pb.MAPB
        LEFT JOIN CHUCVU cv ON n.MACV = cv.MACV
        LEFT JOIN VAITRO v ON t.MAVAITRO = v.MAVAITRO
        WHERE t.MATK = p_matk;
END;
/

-- Lấy danh sách phòng ban
CREATE OR REPLACE PROCEDURE SP_LAY_DANHSACH_PHONGBAN(
    p_cursor OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_cursor FOR
        SELECT MAPB, TENPB, MOTA, NGAYTAO,
               (SELECT COUNT(*) FROM NGUOIDUNG n WHERE n.MAPB = pb.MAPB) AS SO_NHANVIEN
        FROM PHONGBAN pb
        ORDER BY TENPB;
END;
/

-- Lấy danh sách chức vụ
CREATE OR REPLACE PROCEDURE SP_LAY_DANHSACH_CHUCVU(
    p_cursor OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_cursor FOR
        SELECT MACV, TENCV, CAPBAC, MOTA,
               (SELECT COUNT(*) FROM NGUOIDUNG n WHERE n.MACV = cv.MACV) AS SO_NHANVIEN
        FROM CHUCVU cv
        ORDER BY CAPBAC DESC, TENCV;
END;
/

-- ============================================================================
-- 12) PROCEDURE GHI AUDIT LOG
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_WRITE_AUDIT_LOG(
    p_matk VARCHAR2,
    p_action VARCHAR2,
    p_target VARCHAR2,
    p_securitylabel NUMBER DEFAULT 0,
    p_details CLOB DEFAULT NULL,
    p_status VARCHAR2 DEFAULT 'SUCCESS'
) AS
BEGIN
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL, DETAILS, STATUS, SESSION_ID, IP_ADDRESS)
    VALUES(p_matk, p_action, p_target, p_securitylabel, p_details, p_status, 
           SESSION_CTX_PKG.GET_SESSION_ID, SESSION_CTX_PKG.GET_CLIENT_IP);
    COMMIT;
END;
/

-- ============================================================================
-- 13) PROCEDURE LƯU KHÓA MÃ HÓA
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_LUU_ENCRYPTION_KEY(
    p_matk VARCHAR2,
    p_key_type VARCHAR2,
    p_key_value CLOB,
    p_expires_at TIMESTAMP DEFAULT NULL
) AS
BEGIN
    -- Vô hiệu hóa key cũ cùng loại
    UPDATE ENCRYPTION_KEYS 
    SET IS_ACTIVE = 0 
    WHERE MATK = p_matk AND KEY_TYPE = p_key_type;
    
    -- Thêm key mới
    INSERT INTO ENCRYPTION_KEYS(MATK, KEY_TYPE, KEY_VALUE, EXPIRES_AT)
    VALUES(p_matk, p_key_type, p_key_value, p_expires_at);
    
    -- Cập nhật public key trong TAIKHOAN nếu là RSA_PUBLIC
    IF p_key_type = 'RSA_PUBLIC' THEN
        UPDATE TAIKHOAN SET PUBLIC_KEY = p_key_value WHERE MATK = p_matk;
    END IF;
    
    COMMIT;
END;
/

-- ============================================================================
-- 14) PACKAGE MÃ HÓA CƠ SỞ DỮ LIỆU - DBMS_CRYPTO
-- Bao gồm 3 loại:
--   1. Mã hóa đối xứng (Symmetric - AES-256)
--   2. Mã hóa bất đối xứng (Asymmetric - RSA qua ENCRYPTION_KEYS)
--   3. Mã hóa lai (Hybrid - AES + RSA)
-- ============================================================================

CREATE OR REPLACE PACKAGE DB_ENCRYPTION_PKG AS
    -- ========== 1. MÃ HÓA ĐỐI XỨNG (SYMMETRIC - AES-256-CBC) ==========
    -- Dùng cho: Mã hóa nội dung tin nhắn, dữ liệu trong database
    
    -- Khóa AES mặc định cho database (256-bit = 32 bytes)
    -- Trong production: Lưu trữ an toàn trong Oracle Wallet hoặc HSM
    FUNCTION GET_DEFAULT_AES_KEY RETURN RAW;
    
    -- Mã hóa AES-256-CBC
    FUNCTION AES_ENCRYPT(p_plaintext IN VARCHAR2) RETURN RAW;
    FUNCTION AES_ENCRYPT_RAW(p_plaindata IN RAW) RETURN RAW;
    
    -- Giải mã AES-256-CBC
    FUNCTION AES_DECRYPT(p_cipherdata IN RAW) RETURN VARCHAR2;
    FUNCTION AES_DECRYPT_RAW(p_cipherdata IN RAW) RETURN RAW;
    
    -- ========== 2. MÃ HÓA BẤT ĐỐI XỨNG (ASYMMETRIC - RSA) ==========
    -- Dùng cho: Chữ ký số, trao đổi khóa an toàn
    -- Lưu ý: Oracle DBMS_CRYPTO không hỗ trợ RSA trực tiếp
    --        Sử dụng bảng ENCRYPTION_KEYS để lưu trữ và quản lý keys
    
    -- Lấy public key của user
    FUNCTION GET_PUBLIC_KEY(p_matk IN VARCHAR2) RETURN CLOB;
    
    -- Lưu RSA key pair
    PROCEDURE SAVE_KEY_PAIR(p_matk IN VARCHAR2, p_public_key IN CLOB, p_private_key IN CLOB);
    
    -- ========== 3. MÃ HÓA LAI (HYBRID - AES + RSA) ==========
    -- Dùng cho: Mã hóa tin nhắn end-to-end với session key
    
    -- Tạo session key ngẫu nhiên và mã hóa tin nhắn
    FUNCTION HYBRID_ENCRYPT(p_plaintext IN VARCHAR2, p_matk IN VARCHAR2) RETURN CLOB;
    
    -- Giải mã tin nhắn đã mã hóa hybrid
    FUNCTION HYBRID_DECRYPT(p_encrypted_package IN CLOB, p_matk IN VARCHAR2) RETURN VARCHAR2;
    
    -- ========== UTILITY FUNCTIONS ==========
    -- Tạo key ngẫu nhiên
    FUNCTION GENERATE_RANDOM_KEY(p_length IN NUMBER DEFAULT 32) RETURN RAW;
    
    -- Hash SHA-256
    FUNCTION HASH_SHA256(p_data IN VARCHAR2) RETURN RAW;
    FUNCTION HASH_SHA256_HEX(p_data IN VARCHAR2) RETURN VARCHAR2;
    
    -- HMAC cho xác thực tin nhắn
    FUNCTION COMPUTE_HMAC(p_data IN VARCHAR2, p_key IN RAW) RETURN RAW;
    
END DB_ENCRYPTION_PKG;
/

CREATE OR REPLACE PACKAGE BODY DB_ENCRYPTION_PKG AS

    -- Khóa AES mặc định (32 bytes = 256-bit)
    -- Trong production: Không hardcode, lưu trong Oracle Wallet
    g_default_aes_key RAW(32) := UTL_RAW.CAST_TO_RAW('ChatApp_DB_AES_Key_256bits!!');
    
    -- IV cho AES-CBC (16 bytes)
    g_default_iv RAW(16) := UTL_RAW.CAST_TO_RAW('ChatApp_DB_IV!!');
    
    -- AES-256-CBC với PKCS5 padding
    g_aes_type PLS_INTEGER := DBMS_CRYPTO.ENCRYPT_AES256 + DBMS_CRYPTO.CHAIN_CBC + DBMS_CRYPTO.PAD_PKCS5;

    -- ========== 1. MÃ HÓA ĐỐI XỨNG (SYMMETRIC) ==========
    
    FUNCTION GET_DEFAULT_AES_KEY RETURN RAW IS
    BEGIN
        RETURN g_default_aes_key;
    END;

    FUNCTION AES_ENCRYPT(p_plaintext IN VARCHAR2) RETURN RAW IS
        v_plaindata RAW(32767);
    BEGIN
        IF p_plaintext IS NULL OR LENGTH(p_plaintext) = 0 THEN
            RETURN NULL;
        END IF;
        
        v_plaindata := UTL_RAW.CAST_TO_RAW(p_plaintext);
        
        RETURN DBMS_CRYPTO.ENCRYPT(
            src => v_plaindata,
            typ => g_aes_type,
            key => g_default_aes_key,
            iv  => g_default_iv
        );
    END;
    
    FUNCTION AES_ENCRYPT_RAW(p_plaindata IN RAW) RETURN RAW IS
    BEGIN
        IF p_plaindata IS NULL THEN
            RETURN NULL;
        END IF;
        
        RETURN DBMS_CRYPTO.ENCRYPT(
            src => p_plaindata,
            typ => g_aes_type,
            key => g_default_aes_key,
            iv  => g_default_iv
        );
    END;

    FUNCTION AES_DECRYPT(p_cipherdata IN RAW) RETURN VARCHAR2 IS
        v_decrypted RAW(32767);
    BEGIN
        IF p_cipherdata IS NULL THEN
            RETURN NULL;
        END IF;
        
        v_decrypted := DBMS_CRYPTO.DECRYPT(
            src => p_cipherdata,
            typ => g_aes_type,
            key => g_default_aes_key,
            iv  => g_default_iv
        );
        
        RETURN UTL_RAW.CAST_TO_VARCHAR2(v_decrypted);
    END;
    
    FUNCTION AES_DECRYPT_RAW(p_cipherdata IN RAW) RETURN RAW IS
    BEGIN
        IF p_cipherdata IS NULL THEN
            RETURN NULL;
        END IF;
        
        RETURN DBMS_CRYPTO.DECRYPT(
            src => p_cipherdata,
            typ => g_aes_type,
            key => g_default_aes_key,
            iv  => g_default_iv
        );
    END;

    -- ========== 2. MÃ HÓA BẤT ĐỐI XỨNG (ASYMMETRIC) ==========
    
    FUNCTION GET_PUBLIC_KEY(p_matk IN VARCHAR2) RETURN CLOB IS
        v_public_key CLOB;
    BEGIN
        SELECT KEY_VALUE INTO v_public_key
        FROM ENCRYPTION_KEYS
        WHERE MATK = p_matk 
          AND KEY_TYPE = 'RSA_PUBLIC' 
          AND IS_ACTIVE = 1
          AND (EXPIRES_AT IS NULL OR EXPIRES_AT > SYSTIMESTAMP)
        ORDER BY CREATED_AT DESC
        FETCH FIRST 1 ROW ONLY;
        
        RETURN v_public_key;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            RETURN NULL;
    END;
    
    PROCEDURE SAVE_KEY_PAIR(p_matk IN VARCHAR2, p_public_key IN CLOB, p_private_key IN CLOB) IS
    BEGIN
        -- Vô hiệu hóa keys cũ
        UPDATE ENCRYPTION_KEYS 
        SET IS_ACTIVE = 0 
        WHERE MATK = p_matk AND KEY_TYPE IN ('RSA_PUBLIC', 'RSA_PRIVATE');
        
        -- Lưu public key
        INSERT INTO ENCRYPTION_KEYS(MATK, KEY_TYPE, KEY_VALUE, IS_ACTIVE)
        VALUES(p_matk, 'RSA_PUBLIC', p_public_key, 1);
        
        -- Lưu private key (đã được mã hóa bằng AES trước khi lưu)
        INSERT INTO ENCRYPTION_KEYS(MATK, KEY_TYPE, KEY_VALUE, IS_ACTIVE)
        VALUES(p_matk, 'RSA_PRIVATE', p_private_key, 1);
        
        -- Cập nhật public key trong bảng TAIKHOAN
        UPDATE TAIKHOAN SET PUBLIC_KEY = p_public_key WHERE MATK = p_matk;
        
        -- Ghi audit log
        INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
        VALUES(p_matk, 'GENERATE_RSA_KEYPAIR', p_matk, 3);
        
        COMMIT;
    END;

    -- ========== 3. MÃ HÓA LAI (HYBRID) ==========
    
    FUNCTION HYBRID_ENCRYPT(p_plaintext IN VARCHAR2, p_matk IN VARCHAR2) RETURN CLOB IS
        v_session_key RAW(32);
        v_session_iv RAW(16);
        v_encrypted_data RAW(32767);
        v_public_key CLOB;
        v_result CLOB;
    BEGIN
        -- Tạo session key ngẫu nhiên (AES-256)
        v_session_key := DBMS_CRYPTO.RANDOMBYTES(32);
        v_session_iv := DBMS_CRYPTO.RANDOMBYTES(16);
        
        -- Mã hóa dữ liệu bằng AES với session key
        v_encrypted_data := DBMS_CRYPTO.ENCRYPT(
            src => UTL_RAW.CAST_TO_RAW(p_plaintext),
            typ => g_aes_type,
            key => v_session_key,
            iv  => v_session_iv
        );
        
        -- Lấy public key của người nhận
        v_public_key := GET_PUBLIC_KEY(p_matk);
        
        -- Package format: encrypted_data|session_key|iv|target_matk
        -- Lưu ý: Trong thực tế, session_key sẽ được mã hóa bằng RSA public key
        -- Oracle DBMS_CRYPTO không hỗ trợ RSA, nên session key được lưu dạng hex
        -- Client sẽ xử lý RSA encryption
        v_result := UTL_RAW.CAST_TO_VARCHAR2(
            UTL_ENCODE.BASE64_ENCODE(v_encrypted_data)
        ) || '|' ||
        UTL_RAW.CAST_TO_VARCHAR2(
            UTL_ENCODE.BASE64_ENCODE(v_session_key)
        ) || '|' ||
        UTL_RAW.CAST_TO_VARCHAR2(
            UTL_ENCODE.BASE64_ENCODE(v_session_iv)
        ) || '|' || p_matk;
        
        RETURN v_result;
    END;
    
    FUNCTION HYBRID_DECRYPT(p_encrypted_package IN CLOB, p_matk IN VARCHAR2) RETURN VARCHAR2 IS
        v_parts DBMS_SQL.VARCHAR2_TABLE;
        v_encrypted_data RAW(32767);
        v_session_key RAW(32);
        v_session_iv RAW(16);
        v_decrypted RAW(32767);
        v_delim_pos1 NUMBER;
        v_delim_pos2 NUMBER;
        v_delim_pos3 NUMBER;
    BEGIN
        -- Parse package: encrypted_data|session_key|iv|matk
        v_delim_pos1 := INSTR(p_encrypted_package, '|', 1, 1);
        v_delim_pos2 := INSTR(p_encrypted_package, '|', 1, 2);
        v_delim_pos3 := INSTR(p_encrypted_package, '|', 1, 3);
        
        IF v_delim_pos1 = 0 OR v_delim_pos2 = 0 OR v_delim_pos3 = 0 THEN
            RAISE_APPLICATION_ERROR(-20001, 'Invalid encrypted package format');
        END IF;
        
        -- Decode base64 parts
        v_encrypted_data := UTL_ENCODE.BASE64_DECODE(
            UTL_RAW.CAST_TO_RAW(SUBSTR(p_encrypted_package, 1, v_delim_pos1 - 1))
        );
        v_session_key := UTL_ENCODE.BASE64_DECODE(
            UTL_RAW.CAST_TO_RAW(SUBSTR(p_encrypted_package, v_delim_pos1 + 1, v_delim_pos2 - v_delim_pos1 - 1))
        );
        v_session_iv := UTL_ENCODE.BASE64_DECODE(
            UTL_RAW.CAST_TO_RAW(SUBSTR(p_encrypted_package, v_delim_pos2 + 1, v_delim_pos3 - v_delim_pos2 - 1))
        );
        
        -- Giải mã dữ liệu
        v_decrypted := DBMS_CRYPTO.DECRYPT(
            src => v_encrypted_data,
            typ => g_aes_type,
            key => v_session_key,
            iv  => v_session_iv
        );
        
        RETURN UTL_RAW.CAST_TO_VARCHAR2(v_decrypted);
    END;

    -- ========== UTILITY FUNCTIONS ==========
    
    FUNCTION GENERATE_RANDOM_KEY(p_length IN NUMBER DEFAULT 32) RETURN RAW IS
    BEGIN
        RETURN DBMS_CRYPTO.RANDOMBYTES(p_length);
    END;
    
    FUNCTION HASH_SHA256(p_data IN VARCHAR2) RETURN RAW IS
    BEGIN
        RETURN DBMS_CRYPTO.HASH(
            src => UTL_RAW.CAST_TO_RAW(p_data),
            typ => DBMS_CRYPTO.HASH_SH256
        );
    END;
    
    FUNCTION HASH_SHA256_HEX(p_data IN VARCHAR2) RETURN VARCHAR2 IS
    BEGIN
        RETURN RAWTOHEX(HASH_SHA256(p_data));
    END;
    
    FUNCTION COMPUTE_HMAC(p_data IN VARCHAR2, p_key IN RAW) RETURN RAW IS
    BEGIN
        RETURN DBMS_CRYPTO.MAC(
            src => UTL_RAW.CAST_TO_RAW(p_data),
            typ => DBMS_CRYPTO.HMAC_SH256,
            key => p_key
        );
    END;

END DB_ENCRYPTION_PKG;
/

-- ============================================================================
-- 15) PROCEDURES SỬ DỤNG MÃ HÓA
-- ============================================================================

-- Gửi tin nhắn mã hóa (Symmetric - AES)
CREATE OR REPLACE PROCEDURE SP_GUI_TINNHAN_MAHOA_AES(
    p_mactc VARCHAR2,
    p_matk VARCHAR2,
    p_noidung VARCHAR2,
    p_securitylabel NUMBER DEFAULT 1,
    p_matn OUT NUMBER
) AS
    v_encrypted_content RAW(32767);
BEGIN
    -- Mã hóa nội dung bằng AES-256
    v_encrypted_content := DB_ENCRYPTION_PKG.AES_ENCRYPT(p_noidung);
    
    -- Lưu tin nhắn đã mã hóa
    INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL, IS_ENCRYPTED, ENCRYPTED_CONTENT)
    VALUES(p_mactc, p_matk, 'ENCRYPTED', 'ACTIVE', '[Tin nhắn đã mã hóa AES]', p_securitylabel, 1, v_encrypted_content)
    RETURNING MATN INTO p_matn;
    
    -- Cập nhật thời gian cuộc trò chuyện
    UPDATE CUOCTROCHUYEN SET THOIGIANTINNHANCUOI = SYSTIMESTAMP WHERE MACTC = p_mactc;
    
    -- Ghi audit log
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk, 'SEND_ENCRYPTED_MSG_AES', 'MATN=' || p_matn, p_securitylabel);
    
    COMMIT;
END;
/

-- Gửi tin nhắn mã hóa Hybrid (cho E2E encryption)
CREATE OR REPLACE PROCEDURE SP_GUI_TINNHAN_MAHOA_HYBRID(
    p_mactc VARCHAR2,
    p_matk_sender VARCHAR2,
    p_matk_receiver VARCHAR2,
    p_noidung VARCHAR2,
    p_securitylabel NUMBER DEFAULT 1,
    p_matn OUT NUMBER
) AS
    v_encrypted_package CLOB;
BEGIN
    -- Mã hóa nội dung bằng Hybrid (AES + RSA)
    v_encrypted_package := DB_ENCRYPTION_PKG.HYBRID_ENCRYPT(p_noidung, p_matk_receiver);
    
    -- Lưu tin nhắn đã mã hóa
    INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL, IS_ENCRYPTED, ENCRYPTED_CONTENT)
    VALUES(p_mactc, p_matk_sender, 'ENCRYPTED', 'ACTIVE', '[Tin nhắn E2E Hybrid]', p_securitylabel, 1, 
           UTL_RAW.CAST_TO_RAW(SUBSTR(v_encrypted_package, 1, 2000)))
    RETURNING MATN INTO p_matn;
    
    -- Cập nhật thời gian cuộc trò chuyện
    UPDATE CUOCTROCHUYEN SET THOIGIANTINNHANCUOI = SYSTIMESTAMP WHERE MACTC = p_mactc;
    
    -- Ghi audit log
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk_sender, 'SEND_ENCRYPTED_MSG_HYBRID', 'MATN=' || p_matn || ',TO=' || p_matk_receiver, p_securitylabel);
    
    COMMIT;
END;
/

-- Đọc tin nhắn mã hóa AES
CREATE OR REPLACE FUNCTION FN_DOC_TINNHAN_MAHOA_AES(
    p_matn NUMBER
) RETURN VARCHAR2 AS
    v_encrypted_content RAW(32767);
    v_decrypted VARCHAR2(32767);
BEGIN
    SELECT ENCRYPTED_CONTENT INTO v_encrypted_content
    FROM TINNHAN WHERE MATN = p_matn AND IS_ENCRYPTED = 1;
    
    v_decrypted := DB_ENCRYPTION_PKG.AES_DECRYPT(v_encrypted_content);
    
    RETURN v_decrypted;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN NULL;
    WHEN OTHERS THEN
        RETURN '[Lỗi giải mã: ' || SQLERRM || ']';
END;
/

-- Mã hóa attachment
CREATE OR REPLACE PROCEDURE SP_MAHOA_ATTACHMENT(
    p_attachment_id NUMBER,
    p_content BLOB
) AS
    v_encrypted BLOB;
    v_key RAW(32);
    v_iv RAW(16);
BEGIN
    -- Tạo key ngẫu nhiên cho attachment này
    v_key := DB_ENCRYPTION_PKG.GENERATE_RANDOM_KEY(32);
    v_iv := DB_ENCRYPTION_PKG.GENERATE_RANDOM_KEY(16);
    
    -- Mã hóa nội dung
    DBMS_LOB.CREATETEMPORARY(v_encrypted, TRUE);
    v_encrypted := DBMS_CRYPTO.ENCRYPT(
        src => p_content,
        typ => DBMS_CRYPTO.ENCRYPT_AES256 + DBMS_CRYPTO.CHAIN_CBC + DBMS_CRYPTO.PAD_PKCS5,
        key => v_key,
        iv  => v_iv
    );
    
    -- Cập nhật attachment
    UPDATE ATTACHMENT 
    SET ENCRYPTED_CONTENT = v_encrypted,
        ENCRYPTION_KEY = v_key,
        ENCRYPTION_IV = v_iv,
        IS_ENCRYPTED = 1
    WHERE ATTACH_ID = p_attachment_id;
    
    DBMS_LOB.FREETEMPORARY(v_encrypted);
    COMMIT;
END;
/

-- Lưu RSA public key khi user đăng nhập
CREATE OR REPLACE PROCEDURE SP_REGISTER_PUBLIC_KEY(
    p_matk VARCHAR2,
    p_public_key CLOB
) AS
BEGIN
    -- Vô hiệu hóa key cũ
    UPDATE ENCRYPTION_KEYS 
    SET IS_ACTIVE = 0 
    WHERE MATK = p_matk AND KEY_TYPE = 'RSA_PUBLIC';
    
    -- Lưu key mới
    INSERT INTO ENCRYPTION_KEYS(MATK, KEY_TYPE, KEY_VALUE, IS_ACTIVE)
    VALUES(p_matk, 'RSA_PUBLIC', p_public_key, 1);
    
    -- Cập nhật trong TAIKHOAN
    UPDATE TAIKHOAN SET PUBLIC_KEY = p_public_key WHERE MATK = p_matk;
    
    -- Ghi audit log
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk, 'REGISTER_PUBLIC_KEY', p_matk, 2);
    
    COMMIT;
END;
/

--------------------------------------------------------------------------------
-- HOÀN TẤT PROCEDURES
--------------------------------------------------------------------------------

COMMIT;

SELECT 'Procedures created successfully!' AS TRANG_THAI FROM DUAL;
SELECT COUNT(*) AS SO_PROCEDURE FROM USER_PROCEDURES WHERE OBJECT_TYPE = 'PROCEDURE';
SELECT COUNT(*) AS SO_PACKAGE FROM USER_PROCEDURES WHERE OBJECT_TYPE = 'PACKAGE';

--------------------------------------------------------------------------------
-- KẾT THÚC PROCEDURES.SQL
--------------------------------------------------------------------------------
