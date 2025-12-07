--------------------------------------------------------------------------------
-- SCHEMA_COMPLETE.SQL - PHIÊN BẢN HOÀN CHỈNH
-- Bao gồm: Schema, VPD, FGA, MAC Context, Encryption support, Audit logs
-- 
-- HƯỚNG DẪN THỰC THI:
-- 1. Kết nối với SYS/SYSTEM để chạy PHẦN 1 (tạo user, cấp quyền)
-- 2. Kết nối với SYS để chạy PHẦN 2 (tạo context - yêu cầu SYSDBA)
-- 3. Kết nối với ChatApplication để chạy PHẦN 3 (tạo bảng, procedures)
-- 4. Kết nối với ChatApplication để chạy PHẦN 4 (VPD, FGA policies)
--
-- Lưu ý: Chạy từng phần riêng biệt sau khi kết nối đúng user
--------------------------------------------------------------------------------

--------------------------------------------------------------------------------
-- PHẦN 1: CHẠY VỚI SYS hoặc SYSTEM (SYSDBA)
--------------------------------------------------------------------------------
-- ALTER SESSION SET CONTAINER = ORCLPDB; -- Bỏ comment nếu dùng PDB

-- Xóa user cũ nếu tồn tại (cẩn thận khi dùng trong production!)
-- DROP USER ChatApplication CASCADE;

-- Tạo user và cấp quyền
CREATE USER ChatApplication IDENTIFIED BY 123;

GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE PROCEDURE,
      CREATE VIEW, CREATE TRIGGER, CREATE TYPE, UNLIMITED TABLESPACE
      TO ChatApplication;

-- Cấp quyền bổ sung cho VPD, FGA và Context
GRANT EXECUTE ON DBMS_RLS TO ChatApplication;
GRANT EXECUTE ON DBMS_FGA TO ChatApplication;
GRANT EXECUTE ON DBMS_SESSION TO ChatApplication;
GRANT EXECUTE ON DBMS_CRYPTO TO ChatApplication;  -- Cho mã hóa

ALTER USER ChatApplication QUOTA UNLIMITED ON USERS;

--------------------------------------------------------------------------------
-- PHẦN 2: CHẠY VỚI SYS (SYSDBA) - Tạo context
--------------------------------------------------------------------------------
-- Context phải được tạo bởi SYS vì yêu cầu quyền SYSDBA

CREATE OR REPLACE CONTEXT MAC_CTX USING ChatApplication.MAC_CTX_PKG;

-- Cấp quyền truy cập context cho ChatApplication
GRANT EXECUTE ON DBMS_SESSION TO ChatApplication;

--------------------------------------------------------------------------------
-- PHẦN 3: CHẠY VỚI ChatApplication - Tạo bảng và đối tượng
--------------------------------------------------------------------------------
-- Kết nối với ChatApplication trước khi chạy phần này

-- ============================================================================
-- 3.1) CÁC BẢNG CƠ SỞ
-- ============================================================================

-- Bảng vai trò trong hệ thống
CREATE TABLE VAITRO (
  MAVAITRO  VARCHAR2(20) PRIMARY KEY,
  TENVAITRO VARCHAR2(100),
  CHUCNANG  VARCHAR2(200)
);

-- Bảng tài khoản người dùng
CREATE TABLE TAIKHOAN (
  MATK            VARCHAR2(20) PRIMARY KEY,
  TENTK           VARCHAR2(100) NOT NULL UNIQUE,
  PASSWORD_HASH   VARCHAR2(256) NOT NULL,
  MAVAITRO        VARCHAR2(20),
  CLEARANCELEVEL  NUMBER DEFAULT 1 NOT NULL CHECK (CLEARANCELEVEL BETWEEN 1 AND 5),
  IS_BANNED_GLOBAL NUMBER(1) DEFAULT 0 CHECK (IS_BANNED_GLOBAL IN (0, 1)),
  IS_OTP_VERIFIED NUMBER(1) DEFAULT 0 CHECK (IS_OTP_VERIFIED IN (0, 1)),
  NGAYTAO         TIMESTAMP DEFAULT SYSTIMESTAMP,
  LAST_LOGIN      TIMESTAMP,
  PUBLIC_KEY      CLOB,  -- RSA public key cho mã hóa
  CONSTRAINT FK_TAIKHOAN_VAITRO FOREIGN KEY(MAVAITRO) REFERENCES VAITRO(MAVAITRO)
);

CREATE INDEX IDX_TAIKHOAN_TENTK ON TAIKHOAN(TENTK);

-- Bảng phòng ban
CREATE TABLE PHONGBAN (
  MAPB        VARCHAR2(20) PRIMARY KEY,
  TENPB       VARCHAR2(200) NOT NULL,
  MOTA        VARCHAR2(500),
  NGAYTAO     TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Bảng chức vụ
CREATE TABLE CHUCVU (
  MACV        VARCHAR2(20) PRIMARY KEY,
  TENCV       VARCHAR2(100) NOT NULL,
  CAPBAC      NUMBER DEFAULT 1,  -- Cấp bậc (1 = thấp nhất)
  MOTA        VARCHAR2(500)
);

-- Bảng thông tin người dùng chi tiết
CREATE TABLE NGUOIDUNG (
  MATK        VARCHAR2(20) PRIMARY KEY,
  MAPB        VARCHAR2(20),
  MACV        VARCHAR2(20),
  HOVATEN     VARCHAR2(200),
  EMAIL       VARCHAR2(200),
  SDT         VARCHAR2(20),
  NGAYSINH    DATE,
  DIACHI      VARCHAR2(500),
  AVATAR_URL  VARCHAR2(400),
  BIO         VARCHAR2(1000),
  NGAYCAPNHAT TIMESTAMP DEFAULT SYSTIMESTAMP,
  CONSTRAINT FK_NGUOIDUNG_TAIKHOAN FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE,
  CONSTRAINT FK_NGUOIDUNG_PHONGBAN FOREIGN KEY(MAPB) REFERENCES PHONGBAN(MAPB),
  CONSTRAINT FK_NGUOIDUNG_CHUCVU FOREIGN KEY(MACV) REFERENCES CHUCVU(MACV)
);

CREATE INDEX IDX_NGUOIDUNG_EMAIL ON NGUOIDUNG(EMAIL);

-- ============================================================================
-- 3.2) CÁC BẢNG CUỘC TRÒ CHUYỆN
-- ============================================================================

-- Loại cuộc trò chuyện
CREATE TABLE LOAICTC (
  MALOAICTC   VARCHAR2(20) PRIMARY KEY,
  TENLOAICTC  VARCHAR2(200),
  IS_PRIVATE  VARCHAR2(1) DEFAULT 'N' NOT NULL CHECK (IS_PRIVATE IN ('Y', 'N')),
  MOTA        VARCHAR2(400)
);

-- Cuộc trò chuyện
CREATE TABLE CUOCTROCHUYEN (
  MACTC       VARCHAR2(60) PRIMARY KEY,
  MALOAICTC   VARCHAR2(20),
  TENCTC      VARCHAR2(200),
  NGAYTAO     TIMESTAMP DEFAULT SYSTIMESTAMP,
  NGUOIQL     VARCHAR2(20),
  IS_PRIVATE  VARCHAR2(1) DEFAULT 'N' NOT NULL CHECK (IS_PRIVATE IN ('Y', 'N')),
  CREATED_BY  VARCHAR2(20),
  AVATAR_URL  VARCHAR2(400),
  MOTA        VARCHAR2(1000),
  IS_ENCRYPTED NUMBER(1) DEFAULT 0,  -- Cuộc trò chuyện có mã hóa E2E không
  CONSTRAINT FK_CUOCTROCHUYEN_LOAICTC FOREIGN KEY(MALOAICTC) REFERENCES LOAICTC(MALOAICTC),
  CONSTRAINT FK_NGUOIQL_TAIKHOAN FOREIGN KEY(NGUOIQL) REFERENCES TAIKHOAN(MATK)
);

CREATE INDEX IDX_CUOCTROCHUYEN_NGUOIQL ON CUOCTROCHUYEN(NGUOIQL);

-- Phân quyền trong nhóm
CREATE TABLE PHAN_QUYEN_NHOM (
  MAPHANQUYEN VARCHAR2(20) PRIMARY KEY,
  TENQUYEN    VARCHAR2(100),
  CAN_ADD     NUMBER(1) DEFAULT 0,
  CAN_REMOVE  NUMBER(1) DEFAULT 0,
  CAN_PROMOTE NUMBER(1) DEFAULT 0,
  CAN_DELETE  NUMBER(1) DEFAULT 0,
  CAN_BAN     NUMBER(1) DEFAULT 0,
  CAN_UNBAN   NUMBER(1) DEFAULT 0,
  CAN_MUTE    NUMBER(1) DEFAULT 0,
  CAN_UNMUTE  NUMBER(1) DEFAULT 0
);

-- Thành viên cuộc trò chuyện
CREATE TABLE THANHVIEN (
  MACTC             VARCHAR2(60),
  MATK              VARCHAR2(20),
  NGAYTHAMGIA       TIMESTAMP DEFAULT SYSTIMESTAMP,
  QUYEN             VARCHAR2(100),
  MAPHANQUYEN       VARCHAR2(20),
  IS_BANNED         NUMBER(1) DEFAULT 0,
  IS_MUTED          NUMBER(1) DEFAULT 0,
  DELETED_BY_MEMBER NUMBER(1) DEFAULT 0,
  NICKNAME          VARCHAR2(100),  -- Biệt danh trong nhóm
  CONSTRAINT PK_THANHVIEN PRIMARY KEY(MACTC, MATK),
  CONSTRAINT FK_THANHVIEN_CUOCTROCHUYEN FOREIGN KEY(MACTC) REFERENCES CUOCTROCHUYEN(MACTC) ON DELETE CASCADE,
  CONSTRAINT FK_THANHVIEN_TAIKHOAN FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE,
  CONSTRAINT FK_THANHVIEN_PHANQUYEN FOREIGN KEY(MAPHANQUYEN) REFERENCES PHAN_QUYEN_NHOM(MAPHANQUYEN)
);

CREATE INDEX IDX_THANHVIEN_MATK ON THANHVIEN(MATK);

-- ============================================================================
-- 3.3) CÁC BẢNG TIN NHẮN
-- ============================================================================

-- Loại tin nhắn
CREATE TABLE LOAITN (
  MALOAITN  VARCHAR2(20) PRIMARY KEY,
  TENLOAITN VARCHAR2(100)
);

-- Trạng thái tin nhắn
CREATE TABLE TRANGTHAI (
  MATRANGTHAI  VARCHAR2(20) PRIMARY KEY,
  TENTRANGTHAI VARCHAR2(100)
);

-- Tin nhắn
CREATE TABLE TINNHAN (
  MATN            NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  MACTC           VARCHAR2(60),
  MATK            VARCHAR2(20),
  MALOAITN        VARCHAR2(20),
  MATRANGTHAI     VARCHAR2(20),
  NOIDUNG         CLOB,
  NGAYGUI         TIMESTAMP DEFAULT SYSTIMESTAMP,
  SECURITYLABEL   NUMBER DEFAULT 1 NOT NULL CHECK (SECURITYLABEL BETWEEN 1 AND 5),
  -- Encryption fields
  IS_ENCRYPTED    NUMBER(1) DEFAULT 0,
  ENCRYPTED_KEY   CLOB,      -- AES key đã mã hóa bằng RSA
  ENCRYPTION_IV   VARCHAR2(100),  -- IV cho AES
  SIGNATURE       CLOB,      -- Chữ ký số
  CONSTRAINT FK_TINNHAN_CUOCTROCHUYEN FOREIGN KEY(MACTC) REFERENCES CUOCTROCHUYEN(MACTC) ON DELETE CASCADE,
  CONSTRAINT FK_TINNHAN_TAIKHOAN FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK),
  CONSTRAINT FK_TINNHAN_LOAITN FOREIGN KEY(MALOAITN) REFERENCES LOAITN(MALOAITN),
  CONSTRAINT FK_TINNHAN_TRANGTHAI FOREIGN KEY(MATRANGTHAI) REFERENCES TRANGTHAI(MATRANGTHAI)
);

CREATE INDEX IDX_TINNHAN_MACTC ON TINNHAN(MACTC);
CREATE INDEX IDX_TINNHAN_MATK ON TINNHAN(MATK);
CREATE INDEX IDX_TINNHAN_NGAYGUI ON TINNHAN(NGAYGUI);
CREATE INDEX IDX_TINNHAN_SECURITYLABEL ON TINNHAN(SECURITYLABEL);

-- ============================================================================
-- 3.4) CÁC BẢNG ĐÍNH KÈM VÀ OTP
-- ============================================================================

-- Tệp đính kèm
CREATE TABLE ATTACHMENT (
  ATTACH_ID   NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  MATK        VARCHAR2(20) NOT NULL,
  FILENAME    VARCHAR2(255) NOT NULL,
  MIMETYPE    VARCHAR2(200),
  FILESIZE    NUMBER,
  FILEDATA    BLOB,
  STORAGE_URL VARCHAR2(400),
  UPLOADED_AT TIMESTAMP DEFAULT SYSTIMESTAMP,
  IS_ENCRYPTED NUMBER(1) DEFAULT 0,
  CONSTRAINT FK_ATTACH_TAIKHOAN FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE
);

-- Liên kết tin nhắn - đính kèm
CREATE TABLE TINNHAN_ATTACH (
  MATN      NUMBER,
  ATTACH_ID NUMBER,
  CONSTRAINT PK_TINNHAN_ATTACH PRIMARY KEY(MATN, ATTACH_ID),
  CONSTRAINT FK_TA_MATN FOREIGN KEY(MATN) REFERENCES TINNHAN(MATN) ON DELETE CASCADE,
  CONSTRAINT FK_TA_ATTACH FOREIGN KEY(ATTACH_ID) REFERENCES ATTACHMENT(ATTACH_ID) ON DELETE CASCADE
);

-- Xác thực OTP
CREATE TABLE XACTHUCOTP (
  MAOTP          NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  MATK           VARCHAR2(20),
  EMAIL          VARCHAR2(200),
  MAXTOTP        VARCHAR2(256),
  THOIGIANTAO    TIMESTAMP DEFAULT SYSTIMESTAMP,
  THOIGIANTONTAI TIMESTAMP,
  DAXACMINH      NUMBER(1) DEFAULT 0,
  LOAI           VARCHAR2(50) DEFAULT 'REGISTER',  -- REGISTER, RESET_PASSWORD, LOGIN
  CONSTRAINT FK_XACTHUCOTP_TAIKHOAN FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE
);

-- ============================================================================
-- 3.5) BẢNG AUDIT LOGS (ĐÃ CẢI TIẾN)
-- ============================================================================

CREATE TABLE AUDIT_LOGS (
  LOG_ID        NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  MATK          VARCHAR2(20),
  ACTION        VARCHAR2(200) NOT NULL,
  TARGET        VARCHAR2(500),
  SECURITYLABEL NUMBER DEFAULT 0,
  TIMESTAMP     TIMESTAMP DEFAULT SYSTIMESTAMP,
  IP_ADDRESS    VARCHAR2(50),
  USER_AGENT    VARCHAR2(500),
  DETAILS       CLOB,
  STATUS        VARCHAR2(50) DEFAULT 'SUCCESS'  -- SUCCESS, FAILED, BLOCKED
);

CREATE INDEX IDX_AUDIT_TIMESTAMP ON AUDIT_LOGS(TIMESTAMP);
CREATE INDEX IDX_AUDIT_MATK ON AUDIT_LOGS(MATK);
CREATE INDEX IDX_AUDIT_ACTION ON AUDIT_LOGS(ACTION);

-- ============================================================================
-- 3.6) BẢNG ENCRYPTION KEYS (Quản lý khóa mã hóa)
-- ============================================================================

CREATE TABLE ENCRYPTION_KEYS (
  KEY_ID        NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  MATK          VARCHAR2(20),
  KEY_TYPE      VARCHAR2(20) NOT NULL,  -- RSA_PUBLIC, RSA_PRIVATE, AES_SESSION
  KEY_VALUE     CLOB NOT NULL,
  CREATED_AT    TIMESTAMP DEFAULT SYSTIMESTAMP,
  EXPIRES_AT    TIMESTAMP,
  IS_ACTIVE     NUMBER(1) DEFAULT 1,
  CONSTRAINT FK_ENCKEY_TAIKHOAN FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE
);

CREATE INDEX IDX_ENCKEY_MATK ON ENCRYPTION_KEYS(MATK);

-- ============================================================================
-- 3.7) BẢNG CÀI ĐẶT NGƯỜI DÙNG
-- ============================================================================

CREATE TABLE USER_SETTINGS (
  MATK                VARCHAR2(20) PRIMARY KEY,
  THEME               VARCHAR2(20) DEFAULT 'light',
  LANGUAGE            VARCHAR2(10) DEFAULT 'vi',
  NOTIFICATION_SOUND  NUMBER(1) DEFAULT 1,
  NOTIFICATION_DESKTOP NUMBER(1) DEFAULT 1,
  AUTO_DOWNLOAD_MEDIA NUMBER(1) DEFAULT 1,
  ENCRYPTION_ENABLED  NUMBER(1) DEFAULT 0,
  SHOW_ONLINE_STATUS  NUMBER(1) DEFAULT 1,
  READ_RECEIPTS       NUMBER(1) DEFAULT 1,
  CONSTRAINT FK_SETTINGS_TAIKHOAN FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE
);

-- ============================================================================
-- 3.8) PACKAGE MAC_CTX (Quản lý context bảo mật)
-- ============================================================================

CREATE OR REPLACE PACKAGE MAC_CTX_PKG AS
  PROCEDURE SET_USER_LEVEL(p_user IN VARCHAR2, p_level IN NUMBER);
  PROCEDURE CLEAR_CONTEXT;
  FUNCTION GET_USER_LEVEL RETURN NUMBER;
  FUNCTION GET_USERNAME RETURN VARCHAR2;
END MAC_CTX_PKG;
/

CREATE OR REPLACE PACKAGE BODY MAC_CTX_PKG AS
  PROCEDURE SET_USER_LEVEL(p_user IN VARCHAR2, p_level IN NUMBER) IS
  BEGIN
    DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'USERNAME', p_user);
    DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'USER_LEVEL', TO_CHAR(p_level));
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
END MAC_CTX_PKG;
/

-- Procedure helper để set context
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

--------------------------------------------------------------------------------
-- PHẦN 4: VPD VÀ FGA POLICIES (Chạy với ChatApplication)
--------------------------------------------------------------------------------

-- ============================================================================
-- 4.1) VPD POLICY FUNCTION (Cải tiến - không block khi không có context)
-- ============================================================================

CREATE OR REPLACE FUNCTION TINNHAN_POLICY_FN(
  schema_name IN VARCHAR2,
  table_name  IN VARCHAR2
) RETURN VARCHAR2 AS
  v_user_level NUMBER;
BEGIN
  v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '5'));
  
  -- Nếu có context, áp dụng filter
  IF v_user_level IS NOT NULL AND v_user_level > 0 THEN
    RETURN 'SECURITYLABEL <= ' || v_user_level;
  ELSE
    -- Fallback: cho phép tất cả nếu không có context
    RETURN '1=1';
  END IF;
END;
/

-- Thêm VPD Policy
BEGIN
  -- Drop policy cũ nếu tồn tại
  BEGIN
    DBMS_RLS.DROP_POLICY(
      object_schema   => 'CHATAPPLICATION',
      object_name     => 'TINNHAN',
      policy_name     => 'TINNHAN_MAC_POLICY'
    );
  EXCEPTION WHEN OTHERS THEN NULL;
  END;
  
  -- Tạo policy mới
  DBMS_RLS.ADD_POLICY(
    object_schema   => 'CHATAPPLICATION',
    object_name     => 'TINNHAN',
    policy_name     => 'TINNHAN_MAC_POLICY',
    function_schema => 'CHATAPPLICATION',
    policy_function => 'TINNHAN_POLICY_FN',
    statement_types => 'SELECT',
    enable          => TRUE
  );
END;
/

-- ============================================================================
-- 4.2) FGA POLICY (Fine-Grained Auditing)
-- ============================================================================

BEGIN
  -- Drop FGA policy cũ nếu tồn tại
  BEGIN
    DBMS_FGA.DROP_POLICY(
      object_schema => 'CHATAPPLICATION',
      object_name   => 'TINNHAN',
      policy_name   => 'FGA_TINNHAN_SELECT_AUDIT'
    );
  EXCEPTION WHEN OTHERS THEN NULL;
  END;
  
  -- Tạo FGA policy mới
  DBMS_FGA.ADD_POLICY(
    object_schema   => 'CHATAPPLICATION',
    object_name     => 'TINNHAN',
    policy_name     => 'FGA_TINNHAN_SELECT_AUDIT',
    audit_condition => NULL,
    audit_column    => NULL,
    statement_types => 'SELECT',
    enable          => TRUE
  );
END;
/

-- ============================================================================
-- 4.3) TRIGGERS BẢO MẬT
-- ============================================================================

-- Trigger ngăn chặn write-up
CREATE OR REPLACE TRIGGER TRG_TINNHAN_CHECK_WRITE_UP
BEFORE INSERT OR UPDATE ON TINNHAN
FOR EACH ROW
DECLARE
  v_user_level NUMBER;
BEGIN
  v_user_level := MAC_CTX_PKG.GET_USER_LEVEL;
  
  IF :NEW.SECURITYLABEL > v_user_level THEN
    RAISE_APPLICATION_ERROR(-20001, 
      'Từ chối ghi: không thể ghi lên nhãn bảo mật cao hơn (Level ' || 
      :NEW.SECURITYLABEL || ' > User Level ' || v_user_level || ')');
  END IF;
END;
/

-- Trigger audit cho tin nhắn
CREATE OR REPLACE TRIGGER TRG_TINNHAN_AUDIT
AFTER INSERT OR UPDATE OR DELETE ON TINNHAN
FOR EACH ROW
DECLARE
  v_user VARCHAR2(200) := MAC_CTX_PKG.GET_USERNAME;
  v_level NUMBER := MAC_CTX_PKG.GET_USER_LEVEL;
  v_action VARCHAR2(200);
  v_target VARCHAR2(400);
  v_label NUMBER;
BEGIN
  IF INSERTING THEN
    v_action := 'INSERT_TINNHAN';
    v_target := 'MATN=' || TO_CHAR(:NEW.MATN) || ',MACTC=' || :NEW.MACTC;
    v_label := :NEW.SECURITYLABEL;
  ELSIF UPDATING THEN
    v_action := 'UPDATE_TINNHAN';
    v_target := 'MATN=' || TO_CHAR(:NEW.MATN);
    v_label := :NEW.SECURITYLABEL;
  ELSIF DELETING THEN
    v_action := 'DELETE_TINNHAN';
    v_target := 'MATN=' || TO_CHAR(:OLD.MATN);
    v_label := NVL(:OLD.SECURITYLABEL, 1);
  END IF;
  
  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
  VALUES (v_user, v_action, v_target, v_label);
END;
/

-- Trigger cho chat riêng tư (giới hạn 2 thành viên)
CREATE OR REPLACE TRIGGER TRG_THANHVIEN_PRIVATE_CHECK_INS
BEFORE INSERT ON THANHVIEN
FOR EACH ROW
DECLARE
  v_is_private VARCHAR2(1);
  v_count NUMBER;
BEGIN
  BEGIN
    SELECT IS_PRIVATE INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = :NEW.MACTC;
  EXCEPTION WHEN NO_DATA_FOUND THEN
    v_is_private := 'N';
  END;

  IF v_is_private = 'Y' THEN
    SELECT COUNT(*) INTO v_count FROM THANHVIEN 
    WHERE MACTC = :NEW.MACTC AND DELETED_BY_MEMBER = 0;
    
    IF v_count >= 2 THEN
      RAISE_APPLICATION_ERROR(-20070, 
        'Chat riêng tư chỉ có thể có đúng 2 thành viên. Không thể thêm người.');
    END IF;
  END IF;
END;
/

-- ============================================================================
-- 4.4) STORED PROCEDURES
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
  
  -- Tạo settings mặc định
  INSERT INTO USER_SETTINGS(MATK) VALUES(p_matk);
  
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

-- Cập nhật thông tin cá nhân (bao gồm phòng ban, chức vụ)
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

-- Tạo cuộc trò chuyện (hỗ trợ cả MATK và TENTK)
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
  
  COMMIT;
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(-20001, 'User not found: ' || p_created_by);
END;
/

-- Gửi tin nhắn (hỗ trợ mã hóa)
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
      v.TENVAITRO AS VAITRO,
      s.THEME,
      s.LANGUAGE,
      s.NOTIFICATION_SOUND,
      s.ENCRYPTION_ENABLED
    FROM TAIKHOAN t
    LEFT JOIN NGUOIDUNG n ON t.MATK = n.MATK
    LEFT JOIN PHONGBAN pb ON n.MAPB = pb.MAPB
    LEFT JOIN CHUCVU cv ON n.MACV = cv.MACV
    LEFT JOIN VAITRO v ON t.MAVAITRO = v.MAVAITRO
    LEFT JOIN USER_SETTINGS s ON t.MATK = s.MATK
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

-- Cập nhật cài đặt người dùng
CREATE OR REPLACE PROCEDURE SP_CAPNHAT_CAIDAT(
  p_matk VARCHAR2,
  p_theme VARCHAR2 DEFAULT NULL,
  p_language VARCHAR2 DEFAULT NULL,
  p_notification_sound NUMBER DEFAULT NULL,
  p_notification_desktop NUMBER DEFAULT NULL,
  p_encryption_enabled NUMBER DEFAULT NULL,
  p_show_online_status NUMBER DEFAULT NULL,
  p_read_receipts NUMBER DEFAULT NULL
) AS
  v_count NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_count FROM USER_SETTINGS WHERE MATK = p_matk;
  
  IF v_count = 0 THEN
    INSERT INTO USER_SETTINGS(MATK, THEME, LANGUAGE, NOTIFICATION_SOUND, 
                              NOTIFICATION_DESKTOP, ENCRYPTION_ENABLED,
                              SHOW_ONLINE_STATUS, READ_RECEIPTS)
    VALUES(p_matk, 
           NVL(p_theme, 'light'), 
           NVL(p_language, 'vi'),
           NVL(p_notification_sound, 1),
           NVL(p_notification_desktop, 1),
           NVL(p_encryption_enabled, 0),
           NVL(p_show_online_status, 1),
           NVL(p_read_receipts, 1));
  ELSE
    UPDATE USER_SETTINGS SET
      THEME = NVL(p_theme, THEME),
      LANGUAGE = NVL(p_language, LANGUAGE),
      NOTIFICATION_SOUND = NVL(p_notification_sound, NOTIFICATION_SOUND),
      NOTIFICATION_DESKTOP = NVL(p_notification_desktop, NOTIFICATION_DESKTOP),
      ENCRYPTION_ENABLED = NVL(p_encryption_enabled, ENCRYPTION_ENABLED),
      SHOW_ONLINE_STATUS = NVL(p_show_online_status, SHOW_ONLINE_STATUS),
      READ_RECEIPTS = NVL(p_read_receipts, READ_RECEIPTS)
    WHERE MATK = p_matk;
  END IF;
  
  COMMIT;
END;
/

-- Lưu khóa mã hóa
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

-- Ghi audit log
CREATE OR REPLACE PROCEDURE SP_WRITE_AUDIT_LOG(
  p_matk VARCHAR2,
  p_action VARCHAR2,
  p_target VARCHAR2,
  p_securitylabel NUMBER DEFAULT 0,
  p_details CLOB DEFAULT NULL,
  p_status VARCHAR2 DEFAULT 'SUCCESS'
) AS
BEGIN
  INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL, DETAILS, STATUS)
  VALUES(p_matk, p_action, p_target, p_securitylabel, p_details, p_status);
  COMMIT;
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

-- Thêm/Xóa thành viên
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
    COMMIT;
  ELSE
    RAISE_APPLICATION_ERROR(-20080, 'Thành viên đã tồn tại trong cuộc trò chuyện.');
  END IF;
END;
/

CREATE OR REPLACE PROCEDURE SP_XOA_THANHVIEN(
  p_mactc VARCHAR2,
  p_matk VARCHAR2
) AS
BEGIN
  DELETE FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
  COMMIT;
END;
/

-- Ban/Unban/Mute/Unmute member trong nhóm
CREATE OR REPLACE PROCEDURE SP_BAN_MEMBER(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
  UPDATE THANHVIEN SET IS_BANNED = 1 WHERE MACTC = p_mactc AND MATK = p_matk;
  COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_UNBAN_MEMBER(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
  UPDATE THANHVIEN SET IS_BANNED = 0 WHERE MACTC = p_mactc AND MATK = p_matk;
  COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_MUTE_MEMBER(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
  UPDATE THANHVIEN SET IS_MUTED = 1 WHERE MACTC = p_mactc AND MATK = p_matk;
  COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_UNMUTE_MEMBER(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
  UPDATE THANHVIEN SET IS_MUTED = 0 WHERE MACTC = p_mactc AND MATK = p_matk;
  COMMIT;
END;
/

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

-- Gửi tin nhắn riêng tư (tự động tạo conversation)
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

-- Xóa cuộc trò chuyện
CREATE OR REPLACE PROCEDURE SP_XOA_CUOCTROCHUYEN(p_mactc VARCHAR2) AS
BEGIN
  DELETE FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
  COMMIT;
END;
/

-- Xóa tài khoản hoàn toàn
CREATE OR REPLACE PROCEDURE SP_XOA_TAIKHOAN(p_matk VARCHAR2) AS
BEGIN
  DELETE FROM AUDIT_LOGS WHERE MATK = p_matk;
  DELETE FROM TAIKHOAN WHERE MATK = p_matk;
  COMMIT;
END;
/

--------------------------------------------------------------------------------
-- HOÀN TẤT SCHEMA
--------------------------------------------------------------------------------

COMMIT;

-- Verify
SELECT 'Schema created successfully!' AS STATUS FROM DUAL;
SELECT COUNT(*) AS TABLE_COUNT FROM USER_TABLES;
SELECT COUNT(*) AS PROCEDURE_COUNT FROM USER_PROCEDURES WHERE OBJECT_TYPE = 'PROCEDURE';
SELECT COUNT(*) AS TRIGGER_COUNT FROM USER_TRIGGERS;

--------------------------------------------------------------------------------
-- KẾT THÚC SCHEMA_COMPLETE.SQL
--------------------------------------------------------------------------------
