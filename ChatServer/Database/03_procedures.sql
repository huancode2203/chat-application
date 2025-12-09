--------------------------------------------------------------------------------
-- 03_PROCEDURES.SQL - CHẠY VỚI ChatApplication
-- Bao gồm: Packages, Procedures cho MAC, Session, CRUD
--------------------------------------------------------------------------------

-- =============================================================================
-- PACKAGE MAC_CTX_PKG - Quản lý MAC Context
-- =============================================================================
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

-- =============================================================================
-- PACKAGE SESSION_CTX_PKG - Quản lý Session
-- =============================================================================
CREATE OR REPLACE PACKAGE SESSION_CTX_PKG AS
    PROCEDURE SET_SESSION(p_session_id VARCHAR2, p_matk VARCHAR2, p_clearance NUMBER);
    PROCEDURE CLEAR_SESSION;
    FUNCTION GET_SESSION_ID RETURN VARCHAR2;
    FUNCTION GET_SESSION_USER RETURN VARCHAR2;
END SESSION_CTX_PKG;
/

CREATE OR REPLACE PACKAGE BODY SESSION_CTX_PKG AS
    PROCEDURE SET_SESSION(p_session_id VARCHAR2, p_matk VARCHAR2, p_clearance NUMBER) IS
    BEGIN
        DBMS_SESSION.SET_CONTEXT('SESSION_CTX', 'SESSION_ID', p_session_id);
        DBMS_SESSION.SET_CONTEXT('SESSION_CTX', 'USER_MATK', p_matk);
        DBMS_SESSION.SET_CONTEXT('SESSION_CTX', 'CLEARANCE', TO_CHAR(p_clearance));
    END;
    
    PROCEDURE CLEAR_SESSION IS
    BEGIN
        DBMS_SESSION.CLEAR_CONTEXT('SESSION_CTX');
    END;
    
    FUNCTION GET_SESSION_ID RETURN VARCHAR2 IS
    BEGIN
        RETURN SYS_CONTEXT('SESSION_CTX', 'SESSION_ID');
    END;
    
    FUNCTION GET_SESSION_USER RETURN VARCHAR2 IS
    BEGIN
        RETURN SYS_CONTEXT('SESSION_CTX', 'USER_MATK');
    END;
END SESSION_CTX_PKG;
/

-- =============================================================================
-- PACKAGE ADMIN_CTX_PKG - Quản lý Admin Context
-- =============================================================================
CREATE OR REPLACE PACKAGE ADMIN_CTX_PKG AS
    PROCEDURE SET_ADMIN(p_matk VARCHAR2, p_clearance NUMBER);
    FUNCTION IS_ADMIN RETURN BOOLEAN;
    FUNCTION GET_ADMIN_LEVEL RETURN NUMBER;
END ADMIN_CTX_PKG;
/

CREATE OR REPLACE PACKAGE BODY ADMIN_CTX_PKG AS
    PROCEDURE SET_ADMIN(p_matk VARCHAR2, p_clearance NUMBER) IS
    BEGIN
        DBMS_SESSION.SET_CONTEXT('ADMIN_CTX', 'ADMIN_MATK', p_matk);
        DBMS_SESSION.SET_CONTEXT('ADMIN_CTX', 'ADMIN_LEVEL', TO_CHAR(p_clearance));
    END;
    
    FUNCTION IS_ADMIN RETURN BOOLEAN IS
        v_level NUMBER;
    BEGIN
        v_level := TO_NUMBER(NVL(SYS_CONTEXT('ADMIN_CTX', 'ADMIN_LEVEL'), '0'));
        RETURN v_level >= 4;
    END;
    
    FUNCTION GET_ADMIN_LEVEL RETURN NUMBER IS
    BEGIN
        RETURN TO_NUMBER(NVL(SYS_CONTEXT('ADMIN_CTX', 'ADMIN_LEVEL'), '0'));
    END;
END ADMIN_CTX_PKG;
/

-- =============================================================================
-- PROCEDURE SET_MAC_CONTEXT - Thiết lập MAC context
-- =============================================================================
CREATE OR REPLACE PROCEDURE SET_MAC_CONTEXT(
    p_matk IN VARCHAR2,
    p_level IN NUMBER DEFAULT NULL
) AS
    v_level NUMBER;
BEGIN
    IF p_level IS NULL THEN
        BEGIN
            SELECT CLEARANCELEVEL INTO v_level FROM TAIKHOAN WHERE MATK = p_matk OR TENTK = p_matk;
        EXCEPTION WHEN NO_DATA_FOUND THEN v_level := 1;
        END;
    ELSE
        v_level := p_level;
    END IF;
    MAC_CTX_PKG.SET_USER_LEVEL(p_matk, v_level);
END;
/

-- =============================================================================
-- TÀI KHOẢN PROCEDURES
-- =============================================================================
CREATE OR REPLACE PROCEDURE SP_TAO_TAIKHOAN(
    p_matk VARCHAR2,
    p_tentk VARCHAR2,
    p_password_hash VARCHAR2,
    p_mavaitro VARCHAR2,
    p_clearance NUMBER,
    p_is_verified NUMBER DEFAULT 1
) AS
BEGIN
    INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
    VALUES(p_matk, p_tentk, p_password_hash, p_mavaitro, p_clearance, p_is_verified);
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES('SYSTEM', 'CREATE_ACCOUNT', p_matk, 0);
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_DOI_MATKHAU(
    p_matk VARCHAR2,
    p_new_password_hash VARCHAR2
) AS
BEGIN
    UPDATE TAIKHOAN SET PASSWORD_HASH = p_new_password_hash WHERE MATK = p_matk OR TENTK = p_matk;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(p_matk, 'CHANGE_PASSWORD', p_matk);
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_BAN_USER_GLOBAL(p_matk VARCHAR2) AS
BEGIN
    UPDATE TAIKHOAN SET IS_BANNED_GLOBAL = 1 WHERE MATK = p_matk OR TENTK = p_matk;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(MAC_CTX_PKG.GET_USERNAME, 'BAN_USER', p_matk);
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_UNBAN_USER_GLOBAL(p_matk VARCHAR2) AS
BEGIN
    UPDATE TAIKHOAN SET IS_BANNED_GLOBAL = 0 WHERE MATK = p_matk OR TENTK = p_matk;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(MAC_CTX_PKG.GET_USERNAME, 'UNBAN_USER', p_matk);
    COMMIT;
END;
/

-- =============================================================================
-- CUỘC TRÒ CHUYỆN PROCEDURES
-- =============================================================================
CREATE OR REPLACE PROCEDURE SP_TAO_CUOCTROCHUYEN(
    p_mactc VARCHAR2, p_maloaictc VARCHAR2, p_tenctc VARCHAR2,
    p_nguoiql VARCHAR2, p_is_private VARCHAR2, p_created_by VARCHAR2
) AS
    v_matk VARCHAR2(20);
BEGIN
    SELECT MATK INTO v_matk FROM TAIKHOAN WHERE MATK = p_created_by OR TENTK = p_created_by FETCH FIRST 1 ROW ONLY;
    
    INSERT INTO CUOCTROCHUYEN(MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY)
    VALUES(p_mactc, p_maloaictc, p_tenctc, v_matk, p_is_private, v_matk);
    
    INSERT INTO THANHVIEN(MACTC, MATK, QUYEN, MAPHANQUYEN)
    VALUES(p_mactc, v_matk, 'owner', 'OWNER');
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(v_matk, 'CREATE_CONVERSATION', p_mactc);
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_XOA_CUOCTROCHUYEN(p_mactc VARCHAR2) AS
BEGIN
    DELETE FROM CUOCTROCHUYEN WHERE MACTC = p_mactc;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(MAC_CTX_PKG.GET_USERNAME, 'DELETE_CONVERSATION', p_mactc);
    COMMIT;
END;
/

-- =============================================================================
-- THÀNH VIÊN PROCEDURES
-- =============================================================================
CREATE OR REPLACE PROCEDURE SP_THEM_THANHVIEN(
    p_mactc VARCHAR2, p_matk VARCHAR2,
    p_quyen VARCHAR2 DEFAULT 'member', p_maphanquyen VARCHAR2 DEFAULT 'MEMBER'
) AS
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk AND DELETED_BY_MEMBER = 0;
    IF v_count = 0 THEN
        INSERT INTO THANHVIEN(MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES(p_mactc, p_matk, p_quyen, p_maphanquyen);
        INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(MAC_CTX_PKG.GET_USERNAME, 'ADD_MEMBER', p_mactc || ':' || p_matk);
        COMMIT;
    END IF;
END;
/

CREATE OR REPLACE PROCEDURE SP_XOA_THANHVIEN(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
    DELETE FROM THANHVIEN WHERE MACTC = p_mactc AND MATK = p_matk;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(MAC_CTX_PKG.GET_USERNAME, 'REMOVE_MEMBER', p_mactc || ':' || p_matk);
    COMMIT;
END;
/

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

-- =============================================================================
-- TIN NHẮN PROCEDURES
-- =============================================================================
CREATE OR REPLACE PROCEDURE SP_GUI_TINNHAN(
    p_mactc VARCHAR2, p_matk VARCHAR2, p_noidung CLOB, p_securitylabel NUMBER, p_matn OUT NUMBER
) AS
BEGIN
    SET_MAC_CONTEXT(p_matk);
    INSERT INTO TINNHAN(MACTC, MATK, NOIDUNG, SECURITYLABEL, MALOAITN, MATRANGTHAI)
    VALUES(p_mactc, p_matk, p_noidung, p_securitylabel, 'TEXT', 'ACTIVE')
    RETURNING MATN INTO p_matn;
    UPDATE CUOCTROCHUYEN SET THOIGIANTINNHANCUOI = SYSTIMESTAMP WHERE MACTC = p_mactc;
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_GUI_TINNHAN_WITH_ATTACH(
    p_mactc VARCHAR2, p_matk VARCHAR2, p_noidung CLOB, p_securitylabel NUMBER, p_attach_id NUMBER, p_matn OUT NUMBER
) AS
BEGIN
    SET_MAC_CONTEXT(p_matk);
    INSERT INTO TINNHAN(MACTC, MATK, NOIDUNG, SECURITYLABEL, MALOAITN, MATRANGTHAI)
    VALUES(p_mactc, p_matk, p_noidung, p_securitylabel, 'FILE', 'ACTIVE')
    RETURNING MATN INTO p_matn;
    INSERT INTO TINNHAN_ATTACH(MATN, ATTACH_ID) VALUES(p_matn, p_attach_id);
    UPDATE CUOCTROCHUYEN SET THOIGIANTINNHANCUOI = SYSTIMESTAMP WHERE MACTC = p_mactc;
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_XOA_TINNHAN(p_matn NUMBER) AS
BEGIN
    DELETE FROM TINNHAN WHERE MATN = p_matn;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(MAC_CTX_PKG.GET_USERNAME, 'DELETE_MESSAGE', TO_CHAR(p_matn));
    COMMIT;
END;
/

-- =============================================================================
-- ATTACHMENT & AUDIT PROCEDURES
-- =============================================================================
CREATE OR REPLACE PROCEDURE SP_UPLOAD_ATTACHMENT(
    p_matk VARCHAR2, p_filename VARCHAR2, p_mimetype VARCHAR2,
    p_filesize NUMBER, p_filedata BLOB, p_attach_id OUT NUMBER,
    p_is_encrypted NUMBER DEFAULT 0
) AS
BEGIN
    INSERT INTO ATTACHMENT(MATK, FILENAME, MIMETYPE, FILESIZE, FILEDATA, IS_ENCRYPTED)
    VALUES(p_matk, p_filename, p_mimetype, p_filesize, p_filedata, p_is_encrypted)
    RETURNING ATTACH_ID INTO p_attach_id;
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_WRITE_AUDIT_LOG(
    p_matk VARCHAR2, p_action VARCHAR2, p_target VARCHAR2, p_securitylabel NUMBER DEFAULT 0
) AS
BEGIN
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL)
    VALUES(p_matk, p_action, p_target, p_securitylabel);
    COMMIT;
END;
/

CREATE OR REPLACE PROCEDURE SP_TAO_OTP(
    p_matk VARCHAR2, p_email VARCHAR2, p_otp_hash VARCHAR2,
    p_expiry_minutes NUMBER DEFAULT 10, p_maotp OUT NUMBER
) AS
BEGIN
    INSERT INTO XACTHUCOTP (MATK, EMAIL, MAXTOTP, THOIGIANTONTAI)
    VALUES (p_matk, p_email, p_otp_hash, SYSTIMESTAMP + NUMTODSINTERVAL(p_expiry_minutes, 'MINUTE'))
    RETURNING MAOTP INTO p_maotp;
    COMMIT;
END;
/

-- =============================================================================
-- CHUYỂN QUYỀN SỞ HỮU NHÓM
-- =============================================================================
CREATE OR REPLACE PROCEDURE SP_SET_TRUONGNHOM(p_mactc VARCHAR2, p_matk VARCHAR2) AS
BEGIN
    UPDATE THANHVIEN SET QUYEN = 'admin', MAPHANQUYEN = 'ADMIN' WHERE MACTC = p_mactc AND QUYEN = 'owner';
    UPDATE THANHVIEN SET QUYEN = 'owner', MAPHANQUYEN = 'OWNER' WHERE MACTC = p_mactc AND MATK = p_matk;
    UPDATE CUOCTROCHUYEN SET NGUOIQL = p_matk WHERE MACTC = p_mactc;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES(MAC_CTX_PKG.GET_USERNAME, 'TRANSFER_OWNERSHIP', p_mactc);
    COMMIT;
END;
/

-- =============================================================================
-- ĐĂNG KÝ PUBLIC KEY CHO MÃ HÓA
-- =============================================================================
CREATE OR REPLACE PROCEDURE SP_REGISTER_PUBLIC_KEY(
    p_matk VARCHAR2,
    p_public_key CLOB
) AS
BEGIN
    UPDATE ENCRYPTION_KEYS SET IS_ACTIVE = 0 WHERE MATK = p_matk AND KEY_TYPE = 'RSA_PUBLIC';
    INSERT INTO ENCRYPTION_KEYS(MATK, KEY_TYPE, KEY_VALUE) VALUES(p_matk, 'RSA_PUBLIC', p_public_key);
    UPDATE TAIKHOAN SET PUBLIC_KEY = p_public_key WHERE MATK = p_matk;
    COMMIT;
END;
/

-- =============================================================================
-- CẬP NHẬT NGƯỜI DÙNG (ADMIN)
-- =============================================================================
CREATE OR REPLACE PROCEDURE SP_CAPNHAT_NGUOIDUNG_ADMIN(
    p_matk VARCHAR2,
    p_email VARCHAR2 DEFAULT NULL,
    p_hovaten VARCHAR2 DEFAULT NULL,
    p_sdt VARCHAR2 DEFAULT NULL,
    p_clearance NUMBER DEFAULT NULL,
    p_mavaitro VARCHAR2 DEFAULT NULL,
    p_macv VARCHAR2 DEFAULT NULL,
    p_mapb VARCHAR2 DEFAULT NULL
) AS
BEGIN
    IF p_email IS NOT NULL OR p_hovaten IS NOT NULL OR p_sdt IS NOT NULL OR p_macv IS NOT NULL OR p_mapb IS NOT NULL THEN
        MERGE INTO NGUOIDUNG n
        USING (SELECT p_matk AS MATK FROM DUAL) t
        ON (n.MATK = t.MATK)
        WHEN MATCHED THEN
            UPDATE SET 
                EMAIL = NVL(p_email, n.EMAIL),
                HOVATEN = NVL(p_hovaten, n.HOVATEN),
                SDT = NVL(p_sdt, n.SDT),
                MACV = NVL(p_macv, n.MACV),
                MAPB = NVL(p_mapb, n.MAPB),
                NGAYCAPNHAT = SYSTIMESTAMP
        WHEN NOT MATCHED THEN
            INSERT (MATK, EMAIL, HOVATEN, SDT, MACV, MAPB)
            VALUES (p_matk, p_email, p_hovaten, p_sdt, NVL(p_macv, 'CV005'), p_mapb);
    END IF;
    
    IF p_clearance IS NOT NULL OR p_mavaitro IS NOT NULL THEN
        UPDATE TAIKHOAN 
        SET CLEARANCELEVEL = NVL(p_clearance, CLEARANCELEVEL),
            MAVAITRO = NVL(p_mavaitro, MAVAITRO)
        WHERE MATK = p_matk;
    END IF;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET) VALUES('ADMIN', 'UPDATE_USER', p_matk);
    COMMIT;
END;
/

COMMIT;
SELECT 'Procedures created successfully!' AS STATUS FROM DUAL;
