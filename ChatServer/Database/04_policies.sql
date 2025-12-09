--------------------------------------------------------------------------------
-- 04_POLICIES.SQL - CHẠY VỚI ChatApplication
-- Bao gồm: VPD/RLS, MAC Triggers, FGA, Standard Auditing Triggers
--------------------------------------------------------------------------------

-- =============================================================================
-- PHẦN 1: VPD (Virtual Private Database) / RLS (Row Level Security)
-- MAC kết hợp VPD - Điều khiển truy cập bắt buộc
-- =============================================================================

-- VPD Policy Function cho TINNHAN - SELECT (No Read Up)
CREATE OR REPLACE FUNCTION VPD_TINNHAN_SELECT_FN(
    schema_name IN VARCHAR2, table_name IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    IF v_user_level >= 5 THEN RETURN '1=1'; END IF;
    IF v_user_level > 0 THEN RETURN 'SECURITYLABEL <= ' || v_user_level; END IF;
    RETURN '1=1';
END;
/

-- VPD Policy Function cho TINNHAN - INSERT (No Write Up)
CREATE OR REPLACE FUNCTION VPD_TINNHAN_INSERT_FN(
    schema_name IN VARCHAR2, table_name IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    RETURN 'SECURITYLABEL <= ' || v_user_level;
END;
/

-- VPD Policy Function cho CUOCTROCHUYEN
CREATE OR REPLACE FUNCTION VPD_CUOCTROCHUYEN_SELECT_FN(
    schema_name IN VARCHAR2, table_name IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
    v_username VARCHAR2(100);
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    v_username := SYS_CONTEXT('MAC_CTX', 'USERNAME');
    IF v_user_level >= 4 THEN RETURN '1=1'; END IF;
    RETURN 'MIN_CLEARANCE <= ' || v_user_level || 
           ' OR MACTC IN (SELECT MACTC FROM THANHVIEN WHERE MATK = ''' || v_username || ''' AND DELETED_BY_MEMBER = 0)';
END;
/

-- VPD Policy Function cho TAIKHOAN
CREATE OR REPLACE FUNCTION VPD_TAIKHOAN_SELECT_FN(
    schema_name IN VARCHAR2, table_name IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
    v_username VARCHAR2(100);
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    v_username := SYS_CONTEXT('MAC_CTX', 'USERNAME');
    IF v_user_level >= 4 THEN RETURN '1=1'; END IF;
    RETURN 'CLEARANCELEVEL <= ' || v_user_level || ' OR MATK = ''' || v_username || '''';
END;
/

-- Xóa VPD policies cũ nếu tồn tại
BEGIN DBMS_RLS.DROP_POLICY('CHATAPPLICATION','TINNHAN','VPD_TINNHAN_SELECT'); EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN DBMS_RLS.DROP_POLICY('CHATAPPLICATION','TINNHAN','VPD_TINNHAN_INSERT'); EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN DBMS_RLS.DROP_POLICY('CHATAPPLICATION','CUOCTROCHUYEN','VPD_CUOCTROCHUYEN_SELECT'); EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN DBMS_RLS.DROP_POLICY('CHATAPPLICATION','TAIKHOAN','VPD_TAIKHOAN_SELECT'); EXCEPTION WHEN OTHERS THEN NULL; END;
/

-- Thêm VPD Policies
BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema => 'CHATAPPLICATION', object_name => 'TINNHAN',
        policy_name => 'VPD_TINNHAN_SELECT', function_schema => 'CHATAPPLICATION',
        policy_function => 'VPD_TINNHAN_SELECT_FN', statement_types => 'SELECT', enable => TRUE
    );
END;
/

BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema => 'CHATAPPLICATION', object_name => 'TINNHAN',
        policy_name => 'VPD_TINNHAN_INSERT', function_schema => 'CHATAPPLICATION',
        policy_function => 'VPD_TINNHAN_INSERT_FN', statement_types => 'INSERT', enable => TRUE
    );
END;
/

BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema => 'CHATAPPLICATION', object_name => 'CUOCTROCHUYEN',
        policy_name => 'VPD_CUOCTROCHUYEN_SELECT', function_schema => 'CHATAPPLICATION',
        policy_function => 'VPD_CUOCTROCHUYEN_SELECT_FN', statement_types => 'SELECT', enable => TRUE
    );
END;
/

BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema => 'CHATAPPLICATION', object_name => 'TAIKHOAN',
        policy_name => 'VPD_TAIKHOAN_SELECT', function_schema => 'CHATAPPLICATION',
        policy_function => 'VPD_TAIKHOAN_SELECT_FN', statement_types => 'SELECT', enable => TRUE
    );
END;
/

-- Ghi log VPD policies vào ADMIN_POLICY
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_TINNHAN_SELECT', 'VPD', 'TINNHAN', 'MAC: Lọc tin nhắn theo SECURITYLABEL <= CLEARANCELEVEL (No Read Up)', 'VPD_TINNHAN_SELECT_FN', 'SELECT', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_TINNHAN_INSERT', 'VPD', 'TINNHAN', 'MAC: Ngăn gửi tin nhắn mức cao hơn clearance (No Write Up)', 'VPD_TINNHAN_INSERT_FN', 'INSERT', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_CUOCTROCHUYEN_SELECT', 'VPD', 'CUOCTROCHUYEN', 'Lọc cuộc trò chuyện theo quyền tham gia', 'VPD_CUOCTROCHUYEN_SELECT_FN', 'SELECT', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_TAIKHOAN_SELECT', 'VPD', 'TAIKHOAN', 'Ẩn thông tin user có clearance cao hơn', 'VPD_TAIKHOAN_SELECT_FN', 'SELECT', 1);

-- =============================================================================
-- PHẦN 2: MAC + OLS (Oracle Label Security) - Yêu cầu license OLS
-- LƯU Ý: Đã bỏ comment code OLS vì không có license
-- Nếu có OLS license, uncomment và chạy với LBACSYS:
-- BEGIN SA_SYSDBA.CREATE_POLICY(policy_name => 'CHAT_OLS_POLICY', column_name => 'OLS_LABEL'); END;
-- BEGIN SA_COMPONENTS.CREATE_LEVEL('CHAT_OLS_POLICY', 10, 'PUB', 'CONG_KHAI'); END;
-- BEGIN SA_POLICY_ADMIN.APPLY_TABLE_POLICY('CHAT_OLS_POLICY', 'CHATAPPLICATION', 'TINNHAN', 'READ_CONTROL,WRITE_CONTROL'); END;
-- =============================================================================

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('CHAT_OLS_POLICY', 'OLS', 'TINNHAN', 'Oracle Label Security - Nhãn bảo mật (yêu cầu license OLS)', 0);

-- =============================================================================
-- PHẦN 3: STANDARD AUDITING VỚI TRIGGER
-- =============================================================================

-- Trigger Audit cho TAIKHOAN
CREATE OR REPLACE TRIGGER TRG_AUDIT_TAIKHOAN
AFTER INSERT OR UPDATE OR DELETE ON TAIKHOAN
FOR EACH ROW
DECLARE
    v_action VARCHAR2(50);
    v_user VARCHAR2(100) := NVL(MAC_CTX_PKG.GET_USERNAME, USER);
    v_old CLOB; v_new CLOB;
BEGIN
    IF INSERTING THEN v_action := 'INSERT'; v_new := 'MATK=' || :NEW.MATK || ',TENTK=' || :NEW.TENTK;
    ELSIF UPDATING THEN v_action := 'UPDATE'; v_old := 'MATK=' || :OLD.MATK; v_new := 'MATK=' || :NEW.MATK || ',TENTK=' || :NEW.TENTK;
    ELSIF DELETING THEN v_action := 'DELETE'; v_old := 'MATK=' || :OLD.MATK || ',TENTK=' || :OLD.TENTK;
    END IF;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, OLD_VALUE, NEW_VALUE)
    VALUES(v_user, v_action || '_TAIKHOAN', NVL(:NEW.MATK, :OLD.MATK), v_old, v_new);
END;
/

-- Trigger Audit cho TINNHAN
CREATE OR REPLACE TRIGGER TRG_AUDIT_TINNHAN
AFTER INSERT OR UPDATE OR DELETE ON TINNHAN
FOR EACH ROW
DECLARE
    v_user VARCHAR2(200) := MAC_CTX_PKG.GET_USERNAME;
    v_action VARCHAR2(200); v_target VARCHAR2(400); v_label NUMBER;
BEGIN
    IF INSERTING THEN v_action := 'INSERT_TINNHAN'; v_target := 'MATN=' || :NEW.MATN || ',MACTC=' || :NEW.MACTC; v_label := :NEW.SECURITYLABEL;
    ELSIF UPDATING THEN v_action := 'UPDATE_TINNHAN'; v_target := 'MATN=' || :NEW.MATN; v_label := :NEW.SECURITYLABEL;
    ELSIF DELETING THEN v_action := 'DELETE_TINNHAN'; v_target := 'MATN=' || :OLD.MATN; v_label := NVL(:OLD.SECURITYLABEL, 1);
    END IF;
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL) VALUES(v_user, v_action, v_target, v_label);
END;
/

-- Trigger MAC: Ngăn Write-Up (Bell-LaPadula)
CREATE OR REPLACE TRIGGER TRG_TINNHAN_CHECK_WRITE_UP
BEFORE INSERT OR UPDATE ON TINNHAN
FOR EACH ROW
DECLARE
    v_user_level NUMBER;
BEGIN
    v_user_level := MAC_CTX_PKG.GET_USER_LEVEL;
    IF :NEW.SECURITYLABEL > v_user_level THEN
        RAISE_APPLICATION_ERROR(-20001, 
            'MAC Violation: Không thể gửi tin nhắn mức ' || :NEW.SECURITYLABEL || 
            ' (Clearance của bạn: ' || v_user_level || ')');
    END IF;
END;
/

-- Trigger cho Private Chat (chỉ 2 thành viên)
CREATE OR REPLACE TRIGGER TRG_THANHVIEN_PRIVATE_CHECK
BEFORE INSERT ON THANHVIEN
FOR EACH ROW
DECLARE
    v_is_private VARCHAR2(1); v_count NUMBER;
BEGIN
    SELECT NVL(IS_PRIVATE, 'N') INTO v_is_private FROM CUOCTROCHUYEN WHERE MACTC = :NEW.MACTC;
    IF v_is_private = 'Y' THEN
        SELECT COUNT(*) INTO v_count FROM THANHVIEN WHERE MACTC = :NEW.MACTC AND DELETED_BY_MEMBER = 0;
        IF v_count >= 2 THEN
            RAISE_APPLICATION_ERROR(-20070, 'Chat riêng tư chỉ có thể có 2 thành viên.');
        END IF;
    END IF;
EXCEPTION WHEN NO_DATA_FOUND THEN NULL;
END;
/

-- Trigger Audit cho ADMIN_POLICY changes
CREATE OR REPLACE TRIGGER TRG_AUDIT_POLICY_CHANGE
AFTER INSERT OR UPDATE OR DELETE ON ADMIN_POLICY
FOR EACH ROW
DECLARE
    v_action VARCHAR2(50);
    v_user VARCHAR2(100);
    v_old_val VARCHAR2(500);
    v_new_val VARCHAR2(500);
BEGIN
    v_user := NVL(SYS_CONTEXT('MAC_CTX', 'USERNAME'), USER);
    
    IF INSERTING THEN 
        v_action := 'CREATE';
        v_new_val := :NEW.POLICY_NAME || ',Enabled=' || :NEW.IS_ENABLED;
    ELSIF UPDATING THEN 
        v_action := 'UPDATE';
        v_old_val := :OLD.POLICY_NAME || ',Enabled=' || :OLD.IS_ENABLED;
        v_new_val := :NEW.POLICY_NAME || ',Enabled=' || :NEW.IS_ENABLED;
    ELSIF DELETING THEN 
        v_action := 'DELETE';
        v_old_val := :OLD.POLICY_NAME || ',Enabled=' || :OLD.IS_ENABLED;
    END IF;
    
    INSERT INTO POLICY_CHANGE_LOG(POLICY_ID, ACTION, CHANGED_BY, OLD_VALUE, NEW_VALUE)
    VALUES(NVL(:NEW.POLICY_ID, :OLD.POLICY_ID), v_action, v_user, v_old_val, v_new_val);
END;
/

-- Ghi log Triggers vào ADMIN_POLICY
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('TRG_AUDIT_TAIKHOAN', 'DAC', 'TAIKHOAN', 'Trigger ghi nhật ký thay đổi tài khoản', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('TRG_AUDIT_TINNHAN', 'DAC', 'TINNHAN', 'Trigger ghi nhật ký thay đổi tin nhắn', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('TRG_MAC_WRITE_UP_CHECK', 'MAC', 'TINNHAN', 'MAC Trigger: Ngăn gửi tin lên mức cao hơn (Bell-LaPadula No Write Up)', 1);

-- =============================================================================
-- PHẦN 4: FGA (Fine-Grained Auditing)
-- =============================================================================

-- Xóa FGA policies cũ
BEGIN DBMS_FGA.DROP_POLICY('CHATAPPLICATION','TINNHAN','FGA_TINNHAN_SELECT'); EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN DBMS_FGA.DROP_POLICY('CHATAPPLICATION','TINNHAN','FGA_TINNHAN_SENSITIVE'); EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN DBMS_FGA.DROP_POLICY('CHATAPPLICATION','TAIKHOAN','FGA_TAIKHOAN_PASSWORD'); EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN DBMS_FGA.DROP_POLICY('CHATAPPLICATION','AUDIT_LOGS','FGA_AUDIT_ACCESS'); EXCEPTION WHEN OTHERS THEN NULL; END;
/

-- FGA: Audit tất cả SELECT trên TINNHAN
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema => 'CHATAPPLICATION', object_name => 'TINNHAN',
        policy_name => 'FGA_TINNHAN_SELECT', audit_condition => NULL,
        audit_column => 'NOIDUNG,SECURITYLABEL', statement_types => 'SELECT', enable => TRUE
    );
END;
/

-- FGA: Audit truy cập tin nhắn nhạy cảm (level >= 4)
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema => 'CHATAPPLICATION', object_name => 'TINNHAN',
        policy_name => 'FGA_TINNHAN_SENSITIVE', audit_condition => 'SECURITYLABEL >= 4',
        audit_column => 'NOIDUNG', statement_types => 'SELECT,INSERT,UPDATE,DELETE', enable => TRUE
    );
END;
/

-- FGA: Audit truy cập mật khẩu
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema => 'CHATAPPLICATION', object_name => 'TAIKHOAN',
        policy_name => 'FGA_TAIKHOAN_PASSWORD', audit_condition => NULL,
        audit_column => 'PASSWORD_HASH', statement_types => 'SELECT,UPDATE', enable => TRUE
    );
END;
/

-- FGA: Audit truy cập audit logs
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema => 'CHATAPPLICATION', object_name => 'AUDIT_LOGS',
        policy_name => 'FGA_AUDIT_ACCESS', audit_condition => NULL,
        audit_column => NULL, statement_types => 'SELECT,DELETE', enable => TRUE
    );
END;
/

-- Ghi log FGA policies
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_TINNHAN_SELECT', 'FGA', 'TINNHAN', 'Ghi nhật ký chi tiết khi đọc tin nhắn', 'SELECT', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_TINNHAN_SENSITIVE', 'FGA', 'TINNHAN', 'Audit truy cập tin nhắn nhạy cảm (level >= 4)', 'SELECT,INSERT,UPDATE,DELETE', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_TAIKHOAN_PASSWORD', 'FGA', 'TAIKHOAN', 'Audit truy cập mật khẩu', 'SELECT,UPDATE', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_AUDIT_ACCESS', 'FGA', 'AUDIT_LOGS', 'Audit truy cập bảng audit', 'SELECT,DELETE', 1);

-- =============================================================================
-- PHẦN 5: RBAC - Ghi log
-- =============================================================================
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('RBAC_USER_ROLE', 'RBAC', 'TAIKHOAN', 'RBAC: Phân quyền theo vai trò (VT001-Chủ dịch vụ, VT002-Admin, VT003-User)', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('RBAC_GROUP_PERMISSION', 'RBAC', 'PHAN_QUYEN_NHOM', 'RBAC: Phân quyền trong nhóm chat (OWNER, ADMIN, MODERATOR, MEMBER)', 1);

COMMIT;
SELECT 'Policies created successfully!' AS STATUS FROM DUAL;
