--------------------------------------------------------------------------------
-- POLICY.SQL - CHÍNH SÁCH BẢO MẬT CHO CHAT APPLICATION
-- Bao gồm:
--   1. DAC (Discretionary Access Control) - Điều khiển truy cập tùy quyền
--   2. MAC với VPD (Virtual Private Database) - Điều khiển truy cập bắt buộc
--   3. MAC với OLS (Oracle Label Security) - Nhãn bảo mật
--   4. RBAC (Role-Based Access Control) - Điều khiển dựa trên vai trò
--   5. Standard Auditing với Trigger - Ghi nhật ký tiêu chuẩn
--   6. FGA (Fine-Grained Auditing) - Ghi nhật ký chi tiết
--
-- HƯỚNG DẪN THỰC THI:
-- 1. Phần 1-2: Chạy với SYS (SYSDBA)
-- 2. Phần 3-8: Chạy với ChatApplication
--------------------------------------------------------------------------------

--------------------------------------------------------------------------------
-- PHẦN 1: DAC - ĐIỀU KHIỂN TRUY CẬP TÙY QUYỀN (Chạy với SYS)
--------------------------------------------------------------------------------

-- ============================================================================
-- 1.1) TẠO CÁC ROLE CHO RBAC
-- ============================================================================

-- Role cho Chủ dịch vụ (toàn quyền)
CREATE ROLE CHAT_OWNER_ROLE;
GRANT ALL PRIVILEGES TO CHAT_OWNER_ROLE;

-- Role cho Quản trị viên
CREATE ROLE CHAT_ADMIN_ROLE;
GRANT CREATE SESSION TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.TAIKHOAN TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.NGUOIDUNG TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.CUOCTROCHUYEN TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.THANHVIEN TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.TINNHAN TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.PHONGBAN TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.CHUCVU TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.VAITRO TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT ON ChatApplication.AUDIT_LOGS TO CHAT_ADMIN_ROLE;
GRANT SELECT, INSERT, UPDATE, DELETE ON ChatApplication.ADMIN_POLICY TO CHAT_ADMIN_ROLE;
GRANT EXECUTE ON ChatApplication.MAC_CTX_PKG TO CHAT_ADMIN_ROLE;
GRANT EXECUTE ON ChatApplication.ADMIN_CTX_PKG TO CHAT_ADMIN_ROLE;

-- Role cho Người dùng thường
CREATE ROLE CHAT_USER_ROLE;
GRANT CREATE SESSION TO CHAT_USER_ROLE;
GRANT SELECT ON ChatApplication.TAIKHOAN TO CHAT_USER_ROLE;
GRANT SELECT, UPDATE ON ChatApplication.NGUOIDUNG TO CHAT_USER_ROLE;
GRANT SELECT ON ChatApplication.CUOCTROCHUYEN TO CHAT_USER_ROLE;
GRANT SELECT, INSERT, UPDATE ON ChatApplication.THANHVIEN TO CHAT_USER_ROLE;
GRANT SELECT, INSERT ON ChatApplication.TINNHAN TO CHAT_USER_ROLE;
GRANT SELECT ON ChatApplication.PHONGBAN TO CHAT_USER_ROLE;
GRANT SELECT ON ChatApplication.CHUCVU TO CHAT_USER_ROLE;
GRANT SELECT ON ChatApplication.VAITRO TO CHAT_USER_ROLE;
GRANT SELECT, INSERT ON ChatApplication.ATTACHMENT TO CHAT_USER_ROLE;
GRANT EXECUTE ON ChatApplication.MAC_CTX_PKG TO CHAT_USER_ROLE;

-- Role cho Thực tập sinh (quyền hạn chế)
CREATE ROLE CHAT_INTERN_ROLE;
GRANT CREATE SESSION TO CHAT_INTERN_ROLE;
GRANT SELECT ON ChatApplication.TAIKHOAN TO CHAT_INTERN_ROLE;
GRANT SELECT ON ChatApplication.NGUOIDUNG TO CHAT_INTERN_ROLE;
GRANT SELECT ON ChatApplication.CUOCTROCHUYEN TO CHAT_INTERN_ROLE;
GRANT SELECT ON ChatApplication.THANHVIEN TO CHAT_INTERN_ROLE;
GRANT SELECT, INSERT ON ChatApplication.TINNHAN TO CHAT_INTERN_ROLE;
GRANT SELECT ON ChatApplication.PHONGBAN TO CHAT_INTERN_ROLE;
GRANT SELECT ON ChatApplication.CHUCVU TO CHAT_INTERN_ROLE;

-- ============================================================================
-- 1.2) CẤP QUYỀN CHO ChatApplication
-- ============================================================================

-- Quyền VPD và FGA
GRANT EXECUTE ON DBMS_RLS TO ChatApplication;
GRANT EXECUTE ON DBMS_FGA TO ChatApplication;
GRANT EXECUTE ON DBMS_SESSION TO ChatApplication;
GRANT EXECUTE ON DBMS_CRYPTO TO ChatApplication;

-- Quyền Audit
GRANT AUDIT ANY TO ChatApplication;
GRANT AUDIT SYSTEM TO ChatApplication;

--------------------------------------------------------------------------------
-- PHẦN 2: TẠO CONTEXT (Chạy với SYS - SYSDBA)
--------------------------------------------------------------------------------

-- Context cho MAC
CREATE OR REPLACE CONTEXT MAC_CTX USING ChatApplication.MAC_CTX_PKG;

-- Context cho Session
CREATE OR REPLACE CONTEXT SESSION_CTX USING ChatApplication.SESSION_CTX_PKG;

-- Context cho Admin Panel
CREATE OR REPLACE CONTEXT ADMIN_CTX USING ChatApplication.ADMIN_CTX_PKG;

--------------------------------------------------------------------------------
-- PHẦN 3: VPD POLICIES - MAC VỚI VPD (Chạy với ChatApplication)
--------------------------------------------------------------------------------

-- ============================================================================
-- 3.1) VPD POLICY FUNCTION CHO TIN NHẮN
-- Quy tắc: Người dùng chỉ đọc được tin nhắn có SECURITYLABEL <= CLEARANCELEVEL
-- ============================================================================

CREATE OR REPLACE FUNCTION VPD_TINNHAN_SELECT_FN(
    schema_name IN VARCHAR2,
    table_name  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
    v_username VARCHAR2(100);
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '5'));
    v_username := SYS_CONTEXT('MAC_CTX', 'USERNAME');
    
    -- Admin bypass (level 5)
    IF v_user_level >= 5 THEN
        RETURN '1=1';
    END IF;
    
    -- Áp dụng filter theo MAC level
    IF v_user_level IS NOT NULL AND v_user_level > 0 THEN
        RETURN 'SECURITYLABEL <= ' || v_user_level;
    ELSE
        RETURN '1=1';
    END IF;
END;
/

-- VPD Policy cho INSERT (ngăn write-up)
CREATE OR REPLACE FUNCTION VPD_TINNHAN_INSERT_FN(
    schema_name IN VARCHAR2,
    table_name  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    
    -- Chỉ cho phép insert với SECURITYLABEL <= user level
    RETURN 'SECURITYLABEL <= ' || v_user_level;
END;
/

-- VPD Policy cho UPDATE
CREATE OR REPLACE FUNCTION VPD_TINNHAN_UPDATE_FN(
    schema_name IN VARCHAR2,
    table_name  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
    v_username VARCHAR2(100);
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    v_username := SYS_CONTEXT('MAC_CTX', 'USERNAME');
    
    -- Chỉ cho phép update tin nhắn của chính mình hoặc admin
    IF v_user_level >= 4 THEN
        RETURN 'SECURITYLABEL <= ' || v_user_level;
    ELSE
        RETURN 'MATK = ''' || v_username || ''' AND SECURITYLABEL <= ' || v_user_level;
    END IF;
END;
/

-- VPD Policy cho DELETE
CREATE OR REPLACE FUNCTION VPD_TINNHAN_DELETE_FN(
    schema_name IN VARCHAR2,
    table_name  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
    v_username VARCHAR2(100);
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    v_username := SYS_CONTEXT('MAC_CTX', 'USERNAME');
    
    -- Chỉ admin (level 4+) mới được xóa tin nhắn
    IF v_user_level >= 4 THEN
        RETURN 'SECURITYLABEL <= ' || v_user_level;
    ELSE
        -- User thường chỉ xóa tin nhắn của mình
        RETURN 'MATK = ''' || v_username || ''' AND SECURITYLABEL <= ' || v_user_level;
    END IF;
END;
/

-- ============================================================================
-- 3.2) VPD POLICY FUNCTION CHO CUỘC TRÒ CHUYỆN
-- ============================================================================

CREATE OR REPLACE FUNCTION VPD_CUOCTROCHUYEN_SELECT_FN(
    schema_name IN VARCHAR2,
    table_name  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
    v_username VARCHAR2(100);
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    v_username := SYS_CONTEXT('MAC_CTX', 'USERNAME');
    
    -- Admin xem tất cả
    IF v_user_level >= 4 THEN
        RETURN '1=1';
    END IF;
    
    -- User thường chỉ xem cuộc trò chuyện họ tham gia hoặc có MIN_CLEARANCE phù hợp
    RETURN 'MIN_CLEARANCE <= ' || v_user_level || 
           ' OR MACTC IN (SELECT MACTC FROM THANHVIEN WHERE MATK = ''' || v_username || ''' AND DELETED_BY_MEMBER = 0)';
END;
/

-- ============================================================================
-- 3.3) VPD POLICY FUNCTION CHO TÀI KHOẢN
-- ============================================================================

CREATE OR REPLACE FUNCTION VPD_TAIKHOAN_SELECT_FN(
    schema_name IN VARCHAR2,
    table_name  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_user_level NUMBER;
    v_username VARCHAR2(100);
BEGIN
    v_user_level := TO_NUMBER(NVL(SYS_CONTEXT('MAC_CTX', 'USER_LEVEL'), '1'));
    v_username := SYS_CONTEXT('MAC_CTX', 'USERNAME');
    
    -- Admin xem tất cả
    IF v_user_level >= 4 THEN
        RETURN '1=1';
    END IF;
    
    -- User thường chỉ xem thông tin cơ bản (không xem password, clearance cao hơn)
    RETURN 'CLEARANCELEVEL <= ' || v_user_level || ' OR MATK = ''' || v_username || '''';
END;
/

-- ============================================================================
-- 3.4) ĐĂNG KÝ VPD POLICIES
-- ============================================================================

-- Xóa policy cũ nếu tồn tại
BEGIN
    DBMS_RLS.DROP_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_SELECT'
    );
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_RLS.DROP_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_INSERT'
    );
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_RLS.DROP_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_UPDATE'
    );
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_RLS.DROP_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_DELETE'
    );
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- Thêm VPD Policy cho TINNHAN - SELECT
BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_SELECT',
        function_schema => 'CHATAPPLICATION',
        policy_function => 'VPD_TINNHAN_SELECT_FN',
        statement_types => 'SELECT',
        enable          => TRUE
    );
END;
/

-- Thêm VPD Policy cho TINNHAN - INSERT
BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_INSERT',
        function_schema => 'CHATAPPLICATION',
        policy_function => 'VPD_TINNHAN_INSERT_FN',
        statement_types => 'INSERT',
        enable          => TRUE
    );
END;
/

-- Thêm VPD Policy cho TINNHAN - UPDATE
BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_UPDATE',
        function_schema => 'CHATAPPLICATION',
        policy_function => 'VPD_TINNHAN_UPDATE_FN',
        statement_types => 'UPDATE',
        enable          => TRUE
    );
END;
/

-- Thêm VPD Policy cho TINNHAN - DELETE
BEGIN
    DBMS_RLS.ADD_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_DELETE',
        function_schema => 'CHATAPPLICATION',
        policy_function => 'VPD_TINNHAN_DELETE_FN',
        statement_types => 'DELETE',
        enable          => TRUE
    );
END;
/

-- Ghi log policy vào bảng ADMIN_POLICY
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_TINNHAN_SELECT', 'VPD', 'TINNHAN', 'Điều khiển truy cập đọc tin nhắn theo MAC level', 'VPD_TINNHAN_SELECT_FN', 'SELECT', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_TINNHAN_INSERT', 'VPD', 'TINNHAN', 'Ngăn chặn write-up khi thêm tin nhắn', 'VPD_TINNHAN_INSERT_FN', 'INSERT', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_TINNHAN_UPDATE', 'VPD', 'TINNHAN', 'Điều khiển quyền sửa tin nhắn', 'VPD_TINNHAN_UPDATE_FN', 'UPDATE', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_TINNHAN_DELETE', 'VPD', 'TINNHAN', 'Điều khiển quyền xóa tin nhắn', 'VPD_TINNHAN_DELETE_FN', 'DELETE', 1);

COMMIT;

--------------------------------------------------------------------------------
-- PHẦN 4: OLS - ORACLE LABEL SECURITY (Chạy với LBACSYS hoặc SYS)
--------------------------------------------------------------------------------

-- ============================================================================
-- 4.1) TẠO OLS POLICY (Nếu có license OLS)
-- ============================================================================

-- Lưu ý: OLS yêu cầu license riêng. Nếu không có, sử dụng VPD thay thế.
-- Các lệnh sau chỉ chạy được nếu đã cài đặt Oracle Label Security.

/*
-- Kết nối với LBACSYS để tạo policy

-- Tạo OLS Policy
BEGIN
    SA_SYSDBA.CREATE_POLICY(
        policy_name      => 'CHAT_OLS_POLICY',
        column_name      => 'OLS_LABEL',
        default_options  => 'NO_CONTROL'
    );
END;
/

-- Tạo các Level
BEGIN
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name  => 'CHAT_OLS_POLICY',
        level_num    => 10,
        short_name   => 'PUB',
        long_name    => 'CONG_KHAI'
    );
    
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name  => 'CHAT_OLS_POLICY',
        level_num    => 20,
        short_name   => 'INT',
        long_name    => 'NOI_BO'
    );
    
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name  => 'CHAT_OLS_POLICY',
        level_num    => 30,
        short_name   => 'CONF',
        long_name    => 'MAT'
    );
    
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name  => 'CHAT_OLS_POLICY',
        level_num    => 40,
        short_name   => 'SEC',
        long_name    => 'TOI_MAT'
    );
    
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name  => 'CHAT_OLS_POLICY',
        level_num    => 50,
        short_name   => 'TOP',
        long_name    => 'TUYET_MAT'
    );
END;
/

-- Tạo các Compartment (Phòng ban)
BEGIN
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name       => 'CHAT_OLS_POLICY',
        comp_num          => 100,
        short_name        => 'BGD',
        long_name         => 'BAN_GIAM_DOC'
    );
    
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name       => 'CHAT_OLS_POLICY',
        comp_num          => 110,
        short_name        => 'KT',
        long_name         => 'KE_TOAN'
    );
    
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name       => 'CHAT_OLS_POLICY',
        comp_num          => 120,
        short_name        => 'KD',
        long_name         => 'KINH_DOANH'
    );
    
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name       => 'CHAT_OLS_POLICY',
        comp_num          => 130,
        short_name        => 'IT',
        long_name         => 'CONG_NGHE'
    );
END;
/

-- Apply policy cho bảng TINNHAN
BEGIN
    SA_POLICY_ADMIN.APPLY_TABLE_POLICY(
        policy_name    => 'CHAT_OLS_POLICY',
        schema_name    => 'CHATAPPLICATION',
        table_name     => 'TINNHAN',
        table_options  => 'READ_CONTROL,WRITE_CONTROL'
    );
END;
/
*/

-- Ghi log OLS policy
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('CHAT_OLS_POLICY', 'OLS', 'TINNHAN', 'Oracle Label Security - Nhãn bảo mật đa cấp cho tin nhắn (yêu cầu license OLS)', 0);

COMMIT;

--------------------------------------------------------------------------------
-- PHẦN 5: RBAC - ĐIỀU KHIỂN TRUY CẬP DỰA TRÊN VAI TRÒ
--------------------------------------------------------------------------------

-- ============================================================================
-- 5.1) PROCEDURES QUẢN LÝ RBAC
-- ============================================================================

-- Kiểm tra quyền người dùng
CREATE OR REPLACE FUNCTION FN_KIEM_TRA_QUYEN(
    p_matk IN VARCHAR2,
    p_quyen IN VARCHAR2
) RETURN NUMBER AS
    v_mavaitro VARCHAR2(20);
    v_chucnang VARCHAR2(500);
BEGIN
    SELECT t.MAVAITRO, v.CHUCNANG 
    INTO v_mavaitro, v_chucnang
    FROM TAIKHOAN t
    JOIN VAITRO v ON t.MAVAITRO = v.MAVAITRO
    WHERE t.MATK = p_matk;
    
    -- Chủ dịch vụ có tất cả quyền
    IF v_mavaitro = 'VT001' THEN
        RETURN 1;
    END IF;
    
    -- Kiểm tra chức năng cụ thể
    IF INSTR(UPPER(v_chucnang), UPPER(p_quyen)) > 0 THEN
        RETURN 1;
    END IF;
    
    RETURN 0;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
END;
/

-- Kiểm tra quyền trong nhóm chat
CREATE OR REPLACE FUNCTION FN_KIEM_TRA_QUYEN_NHOM(
    p_mactc IN VARCHAR2,
    p_matk IN VARCHAR2,
    p_quyen IN VARCHAR2
) RETURN NUMBER AS
    v_maphanquyen VARCHAR2(20);
    v_can_action NUMBER;
BEGIN
    SELECT MAPHANQUYEN INTO v_maphanquyen
    FROM THANHVIEN
    WHERE MACTC = p_mactc AND MATK = p_matk AND DELETED_BY_MEMBER = 0;
    
    -- Owner có tất cả quyền
    IF v_maphanquyen = 'OWNER' THEN
        RETURN 1;
    END IF;
    
    -- Kiểm tra quyền cụ thể
    EXECUTE IMMEDIATE 
        'SELECT ' || p_quyen || ' FROM PHAN_QUYEN_NHOM WHERE MAPHANQUYEN = :1'
        INTO v_can_action
        USING v_maphanquyen;
    
    RETURN v_can_action;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
    WHEN OTHERS THEN
        RETURN 0;
END;
/

-- Gán vai trò cho người dùng
CREATE OR REPLACE PROCEDURE SP_GAN_VAITRO(
    p_matk VARCHAR2,
    p_mavaitro VARCHAR2
) AS
    v_old_vaitro VARCHAR2(20);
BEGIN
    SELECT MAVAITRO INTO v_old_vaitro FROM TAIKHOAN WHERE MATK = p_matk;
    
    UPDATE TAIKHOAN SET MAVAITRO = p_mavaitro WHERE MATK = p_matk;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, OLD_VALUE, NEW_VALUE)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'ASSIGN_ROLE', p_matk, v_old_vaitro, p_mavaitro);
    
    -- Ghi log thay đổi policy
    INSERT INTO POLICY_CHANGE_LOG(POLICY_ID, ACTION, CHANGED_BY, OLD_VALUE, NEW_VALUE, REASON)
    SELECT POLICY_ID, 'UPDATE', MAC_CTX_PKG.GET_USERNAME, v_old_vaitro, p_mavaitro, 'Thay đổi vai trò người dùng'
    FROM ADMIN_POLICY WHERE POLICY_TYPE = 'RBAC' AND ROWNUM = 1;
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20001, 'Không tìm thấy tài khoản: ' || p_matk);
END;
/

-- Ghi log RBAC policy
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('RBAC_USER_ROLE', 'RBAC', 'TAIKHOAN', 'Phân quyền người dùng dựa trên vai trò (VT001-Chủ dịch vụ, VT002-Quản trị viên, VT003-Người dùng)', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('RBAC_GROUP_PERMISSION', 'RBAC', 'PHAN_QUYEN_NHOM', 'Phân quyền thành viên trong nhóm chat (OWNER, ADMIN, MODERATOR, MEMBER)', 1);

COMMIT;

--------------------------------------------------------------------------------
-- PHẦN 6: STANDARD AUDITING VỚI TRIGGER
--------------------------------------------------------------------------------

-- ============================================================================
-- 6.1) TRIGGER AUDIT CHO TÀI KHOẢN
-- ============================================================================

CREATE OR REPLACE TRIGGER TRG_AUDIT_TAIKHOAN
AFTER INSERT OR UPDATE OR DELETE ON TAIKHOAN
FOR EACH ROW
DECLARE
    v_action VARCHAR2(50);
    v_user VARCHAR2(100) := NVL(MAC_CTX_PKG.GET_USERNAME, USER);
    v_old_value CLOB;
    v_new_value CLOB;
BEGIN
    IF INSERTING THEN
        v_action := 'INSERT';
        v_new_value := 'MATK=' || :NEW.MATK || ',TENTK=' || :NEW.TENTK || ',MAVAITRO=' || :NEW.MAVAITRO || ',CLEARANCE=' || :NEW.CLEARANCELEVEL;
    ELSIF UPDATING THEN
        v_action := 'UPDATE';
        v_old_value := 'MATK=' || :OLD.MATK || ',TENTK=' || :OLD.TENTK || ',MAVAITRO=' || :OLD.MAVAITRO || ',CLEARANCE=' || :OLD.CLEARANCELEVEL;
        v_new_value := 'MATK=' || :NEW.MATK || ',TENTK=' || :NEW.TENTK || ',MAVAITRO=' || :NEW.MAVAITRO || ',CLEARANCE=' || :NEW.CLEARANCELEVEL;
    ELSIF DELETING THEN
        v_action := 'DELETE';
        v_old_value := 'MATK=' || :OLD.MATK || ',TENTK=' || :OLD.TENTK || ',MAVAITRO=' || :OLD.MAVAITRO || ',CLEARANCE=' || :OLD.CLEARANCELEVEL;
    END IF;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, SECURITYLABEL, OLD_VALUE, NEW_VALUE)
    VALUES(v_user, v_action || '_TAIKHOAN', NVL(:NEW.MATK, :OLD.MATK), 0, v_old_value, v_new_value);
END;
/

-- ============================================================================
-- 6.2) TRIGGER AUDIT CHO TIN NHẮN
-- ============================================================================

CREATE OR REPLACE TRIGGER TRG_AUDIT_TINNHAN
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
    VALUES(v_user, v_action, v_target, v_label);
END;
/

-- ============================================================================
-- 6.3) TRIGGER NGĂN CHẶN WRITE-UP (MAC)
-- ============================================================================

CREATE OR REPLACE TRIGGER TRG_TINNHAN_CHECK_WRITE_UP
BEFORE INSERT OR UPDATE ON TINNHAN
FOR EACH ROW
DECLARE
    v_user_level NUMBER;
BEGIN
    v_user_level := MAC_CTX_PKG.GET_USER_LEVEL;
    
    -- Không cho phép ghi lên mức cao hơn
    IF :NEW.SECURITYLABEL > v_user_level THEN
        RAISE_APPLICATION_ERROR(-20001, 
            'Từ chối ghi: không thể ghi lên nhãn bảo mật cao hơn (Label ' || 
            :NEW.SECURITYLABEL || ' > User Level ' || v_user_level || ')');
    END IF;
END;
/

-- ============================================================================
-- 6.4) TRIGGER CHO CHAT RIÊNG TƯ
-- ============================================================================

CREATE OR REPLACE TRIGGER TRG_THANHVIEN_PRIVATE_CHECK
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
-- 6.5) TRIGGER AUDIT CHO POLICY CHANGES
-- ============================================================================

CREATE OR REPLACE TRIGGER TRG_AUDIT_POLICY_CHANGE
AFTER INSERT OR UPDATE OR DELETE ON ADMIN_POLICY
FOR EACH ROW
DECLARE
    v_action VARCHAR2(50);
    v_user VARCHAR2(100) := NVL(MAC_CTX_PKG.GET_USERNAME, USER);
BEGIN
    IF INSERTING THEN
        v_action := 'CREATE';
    ELSIF UPDATING THEN
        v_action := 'UPDATE';
    ELSIF DELETING THEN
        v_action := 'DELETE';
    END IF;
    
    INSERT INTO POLICY_CHANGE_LOG(POLICY_ID, ACTION, CHANGED_BY, OLD_VALUE, NEW_VALUE)
    VALUES(
        NVL(:NEW.POLICY_ID, :OLD.POLICY_ID),
        v_action,
        v_user,
        CASE WHEN UPDATING OR DELETING THEN 
            :OLD.POLICY_NAME || ',' || :OLD.POLICY_TYPE || ',Enabled=' || :OLD.IS_ENABLED 
        END,
        CASE WHEN INSERTING OR UPDATING THEN 
            :NEW.POLICY_NAME || ',' || :NEW.POLICY_TYPE || ',Enabled=' || :NEW.IS_ENABLED 
        END
    );
END;
/

-- Ghi log Standard Auditing
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('AUDIT_TAIKHOAN_TRIGGER', 'DAC', 'TAIKHOAN', 'Trigger ghi nhật ký thay đổi tài khoản', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('AUDIT_TINNHAN_TRIGGER', 'DAC', 'TINNHAN', 'Trigger ghi nhật ký thay đổi tin nhắn', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('MAC_WRITE_UP_CHECK', 'MAC', 'TINNHAN', 'Trigger ngăn chặn ghi lên mức bảo mật cao hơn (No-Write-Up)', 1);

COMMIT;

--------------------------------------------------------------------------------
-- PHẦN 7: FGA - FINE-GRAINED AUDITING
--------------------------------------------------------------------------------

-- ============================================================================
-- 7.1) FGA POLICY CHO TIN NHẮN
-- ============================================================================

-- Xóa FGA policy cũ nếu tồn tại
BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'CHATAPPLICATION',
        object_name   => 'TINNHAN',
        policy_name   => 'FGA_TINNHAN_SELECT'
    );
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'CHATAPPLICATION',
        object_name   => 'TINNHAN',
        policy_name   => 'FGA_TINNHAN_SENSITIVE'
    );
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- FGA cho tất cả SELECT trên TINNHAN
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'FGA_TINNHAN_SELECT',
        audit_condition => NULL,
        audit_column    => 'NOIDUNG,SECURITYLABEL',
        statement_types => 'SELECT',
        enable          => TRUE
    );
END;
/

-- FGA cho truy cập tin nhắn nhạy cảm (level >= 4)
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TINNHAN',
        policy_name     => 'FGA_TINNHAN_SENSITIVE',
        audit_condition => 'SECURITYLABEL >= 4',
        audit_column    => 'NOIDUNG',
        statement_types => 'SELECT,INSERT,UPDATE,DELETE',
        enable          => TRUE
    );
END;
/

-- ============================================================================
-- 7.2) FGA POLICY CHO TÀI KHOẢN
-- ============================================================================

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'CHATAPPLICATION',
        object_name   => 'TAIKHOAN',
        policy_name   => 'FGA_TAIKHOAN_PASSWORD'
    );
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- FGA cho truy cập mật khẩu
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'TAIKHOAN',
        policy_name     => 'FGA_TAIKHOAN_PASSWORD',
        audit_condition => NULL,
        audit_column    => 'PASSWORD_HASH',
        statement_types => 'SELECT,UPDATE',
        enable          => TRUE
    );
END;
/

-- ============================================================================
-- 7.3) FGA POLICY CHO AUDIT LOGS
-- ============================================================================

BEGIN
    DBMS_FGA.DROP_POLICY(
        object_schema => 'CHATAPPLICATION',
        object_name   => 'AUDIT_LOGS',
        policy_name   => 'FGA_AUDIT_ACCESS'
    );
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- FGA cho truy cập audit logs
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'CHATAPPLICATION',
        object_name     => 'AUDIT_LOGS',
        policy_name     => 'FGA_AUDIT_ACCESS',
        audit_condition => NULL,
        audit_column    => NULL,
        statement_types => 'SELECT,DELETE',
        enable          => TRUE
    );
END;
/

-- Ghi log FGA policies
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_TINNHAN_SELECT', 'FGA', 'TINNHAN', 'Ghi nhật ký chi tiết khi đọc tin nhắn', 'SELECT', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_TINNHAN_SENSITIVE', 'FGA', 'TINNHAN', 'Ghi nhật ký khi truy cập tin nhắn nhạy cảm (level >= 4)', 'SELECT,INSERT,UPDATE,DELETE', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_TAIKHOAN_PASSWORD', 'FGA', 'TAIKHOAN', 'Ghi nhật ký khi truy cập mật khẩu', 'SELECT,UPDATE', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_AUDIT_ACCESS', 'FGA', 'AUDIT_LOGS', 'Ghi nhật ký khi truy cập bảng audit', 'SELECT,DELETE', 1);

COMMIT;

--------------------------------------------------------------------------------
-- PHẦN 8: PROCEDURES QUẢN LÝ POLICY CHO ADMIN PANEL
--------------------------------------------------------------------------------

-- ============================================================================
-- 8.1) PROCEDURE TẠO POLICY MỚI
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_ADMIN_TAO_POLICY(
    p_policy_name VARCHAR2,
    p_policy_type VARCHAR2,
    p_table_name VARCHAR2,
    p_description VARCHAR2,
    p_policy_function VARCHAR2 DEFAULT NULL,
    p_statement_types VARCHAR2 DEFAULT NULL,
    p_policy_id OUT NUMBER
) AS
BEGIN
    -- Kiểm tra quyền admin
    IF NOT ADMIN_CTX_PKG.IS_ADMIN THEN
        RAISE_APPLICATION_ERROR(-20403, 'Chỉ quản trị viên mới được tạo policy.');
    END IF;
    
    INSERT INTO ADMIN_POLICY(
        POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, 
        POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED, CREATED_BY
    )
    VALUES(
        p_policy_name, p_policy_type, p_table_name, p_description,
        p_policy_function, p_statement_types, 1, MAC_CTX_PKG.GET_USERNAME
    )
    RETURNING POLICY_ID INTO p_policy_id;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, DETAILS)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'CREATE_POLICY', p_policy_name, p_description);
    
    COMMIT;
END;
/

-- ============================================================================
-- 8.2) PROCEDURE CẬP NHẬT POLICY
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_ADMIN_CAPNHAT_POLICY(
    p_policy_id NUMBER,
    p_description VARCHAR2 DEFAULT NULL,
    p_statement_types VARCHAR2 DEFAULT NULL,
    p_is_enabled NUMBER DEFAULT NULL
) AS
    v_old_desc VARCHAR2(1000);
    v_old_types VARCHAR2(100);
    v_old_enabled NUMBER;
BEGIN
    -- Kiểm tra quyền admin
    IF NOT ADMIN_CTX_PKG.IS_ADMIN THEN
        RAISE_APPLICATION_ERROR(-20403, 'Chỉ quản trị viên mới được cập nhật policy.');
    END IF;
    
    -- Lấy giá trị cũ
    SELECT DESCRIPTION, STATEMENT_TYPES, IS_ENABLED
    INTO v_old_desc, v_old_types, v_old_enabled
    FROM ADMIN_POLICY WHERE POLICY_ID = p_policy_id;
    
    -- Cập nhật
    UPDATE ADMIN_POLICY
    SET DESCRIPTION = NVL(p_description, DESCRIPTION),
        STATEMENT_TYPES = NVL(p_statement_types, STATEMENT_TYPES),
        IS_ENABLED = NVL(p_is_enabled, IS_ENABLED),
        UPDATED_AT = SYSTIMESTAMP
    WHERE POLICY_ID = p_policy_id;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, OLD_VALUE, NEW_VALUE)
    VALUES(
        MAC_CTX_PKG.GET_USERNAME, 
        'UPDATE_POLICY', 
        TO_CHAR(p_policy_id),
        'Desc=' || v_old_desc || ',Types=' || v_old_types || ',Enabled=' || v_old_enabled,
        'Desc=' || NVL(p_description, v_old_desc) || ',Types=' || NVL(p_statement_types, v_old_types) || ',Enabled=' || NVL(p_is_enabled, v_old_enabled)
    );
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20404, 'Không tìm thấy policy với ID: ' || p_policy_id);
END;
/

-- ============================================================================
-- 8.3) PROCEDURE XÓA POLICY
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_ADMIN_XOA_POLICY(
    p_policy_id NUMBER
) AS
    v_policy_name VARCHAR2(100);
    v_policy_type VARCHAR2(50);
BEGIN
    -- Kiểm tra quyền admin
    IF NOT ADMIN_CTX_PKG.IS_ADMIN THEN
        RAISE_APPLICATION_ERROR(-20403, 'Chỉ quản trị viên mới được xóa policy.');
    END IF;
    
    SELECT POLICY_NAME, POLICY_TYPE INTO v_policy_name, v_policy_type
    FROM ADMIN_POLICY WHERE POLICY_ID = p_policy_id;
    
    DELETE FROM ADMIN_POLICY WHERE POLICY_ID = p_policy_id;
    
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET, DETAILS)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'DELETE_POLICY', v_policy_name, 'Type=' || v_policy_type);
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20404, 'Không tìm thấy policy với ID: ' || p_policy_id);
END;
/

-- ============================================================================
-- 8.4) PROCEDURE BẬT/TẮT POLICY
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_ADMIN_BAT_TAT_POLICY(
    p_policy_id NUMBER,
    p_enable NUMBER  -- 1 = bật, 0 = tắt
) AS
    v_policy_name VARCHAR2(100);
    v_policy_type VARCHAR2(50);
    v_table_name VARCHAR2(100);
BEGIN
    -- Kiểm tra quyền admin
    IF NOT ADMIN_CTX_PKG.IS_ADMIN THEN
        RAISE_APPLICATION_ERROR(-20403, 'Chỉ quản trị viên mới được bật/tắt policy.');
    END IF;
    
    SELECT POLICY_NAME, POLICY_TYPE, TABLE_NAME 
    INTO v_policy_name, v_policy_type, v_table_name
    FROM ADMIN_POLICY WHERE POLICY_ID = p_policy_id;
    
    -- Cập nhật trạng thái trong bảng quản lý
    UPDATE ADMIN_POLICY SET IS_ENABLED = p_enable, UPDATED_AT = SYSTIMESTAMP
    WHERE POLICY_ID = p_policy_id;
    
    -- Bật/tắt policy thực tế (VPD)
    IF v_policy_type = 'VPD' THEN
        IF p_enable = 1 THEN
            DBMS_RLS.ENABLE_POLICY('CHATAPPLICATION', v_table_name, v_policy_name, TRUE);
        ELSE
            DBMS_RLS.ENABLE_POLICY('CHATAPPLICATION', v_table_name, v_policy_name, FALSE);
        END IF;
    END IF;
    
    -- Bật/tắt policy thực tế (FGA)
    IF v_policy_type = 'FGA' THEN
        IF p_enable = 1 THEN
            DBMS_FGA.ENABLE_POLICY('CHATAPPLICATION', v_table_name, v_policy_name);
        ELSE
            DBMS_FGA.DISABLE_POLICY('CHATAPPLICATION', v_table_name, v_policy_name);
        END IF;
    END IF;
    
    INSERT INTO POLICY_CHANGE_LOG(POLICY_ID, ACTION, CHANGED_BY, REASON)
    VALUES(p_policy_id, CASE WHEN p_enable = 1 THEN 'ENABLE' ELSE 'DISABLE' END, 
           MAC_CTX_PKG.GET_USERNAME, 'Admin panel action');
    
    COMMIT;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20404, 'Không tìm thấy policy với ID: ' || p_policy_id);
END;
/

-- ============================================================================
-- 8.5) PROCEDURE LẤY DANH SÁCH POLICY
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_ADMIN_LAY_DANHSACH_POLICY(
    p_policy_type VARCHAR2 DEFAULT NULL,
    p_cursor OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_cursor FOR
        SELECT 
            p.POLICY_ID,
            p.POLICY_NAME,
            p.POLICY_TYPE,
            p.TABLE_NAME,
            p.DESCRIPTION,
            p.POLICY_FUNCTION,
            p.STATEMENT_TYPES,
            p.IS_ENABLED,
            p.CREATED_BY,
            p.CREATED_AT,
            p.UPDATED_AT,
            (SELECT COUNT(*) FROM POLICY_CHANGE_LOG l WHERE l.POLICY_ID = p.POLICY_ID) AS CHANGE_COUNT
        FROM ADMIN_POLICY p
        WHERE (p_policy_type IS NULL OR p.POLICY_TYPE = p_policy_type)
        ORDER BY p.POLICY_TYPE, p.POLICY_NAME;
END;
/

-- ============================================================================
-- 8.6) PROCEDURE LẤY LỊCH SỬ THAY ĐỔI POLICY
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_ADMIN_LAY_LICHSU_POLICY(
    p_policy_id NUMBER DEFAULT NULL,
    p_cursor OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_cursor FOR
        SELECT 
            l.LOG_ID,
            l.POLICY_ID,
            p.POLICY_NAME,
            l.ACTION,
            l.CHANGED_BY,
            l.CHANGED_AT,
            l.OLD_VALUE,
            l.NEW_VALUE,
            l.REASON
        FROM POLICY_CHANGE_LOG l
        LEFT JOIN ADMIN_POLICY p ON l.POLICY_ID = p.POLICY_ID
        WHERE (p_policy_id IS NULL OR l.POLICY_ID = p_policy_id)
        ORDER BY l.CHANGED_AT DESC;
END;
/

-- ============================================================================
-- 8.7) PROCEDURE THIẾT LẬP CHẾ ĐỘ ADMIN
-- ============================================================================

CREATE OR REPLACE PROCEDURE SP_SET_ADMIN_MODE(
    p_matk VARCHAR2
) AS
    v_mavaitro VARCHAR2(20);
    v_clearance NUMBER;
BEGIN
    SELECT MAVAITRO, CLEARANCELEVEL INTO v_mavaitro, v_clearance
    FROM TAIKHOAN WHERE MATK = p_matk;
    
    -- Chỉ VT001 (Chủ dịch vụ) hoặc VT002 (Quản trị viên) mới được admin mode
    IF v_mavaitro IN ('VT001', 'VT002') OR v_clearance >= 4 THEN
        ADMIN_CTX_PKG.SET_ADMIN_MODE(p_matk, 1);
        MAC_CTX_PKG.SET_USER_LEVEL(p_matk, v_clearance);
        
        INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET)
        VALUES(p_matk, 'ENTER_ADMIN_MODE', 'ADMIN_PANEL');
        COMMIT;
    ELSE
        RAISE_APPLICATION_ERROR(-20403, 'Bạn không có quyền truy cập Admin Panel.');
    END IF;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RAISE_APPLICATION_ERROR(-20404, 'Không tìm thấy tài khoản: ' || p_matk);
END;
/

-- ============================================================================
-- THÊM POLICIES ĐỂ TEST QUẢN LÝ POLICY
-- ============================================================================

-- VPD Policies cho các bảng khác
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_CUOCTROCHUYEN_SELECT', 'VPD', 'CUOCTROCHUYEN', 'Điều khiển truy cập cuộc trò chuyện theo MAC level', 'VPD_CUOCTROCHUYEN_SELECT_FN', 'SELECT', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_TAIKHOAN_SELECT', 'VPD', 'TAIKHOAN', 'Điều khiển truy cập thông tin tài khoản', 'VPD_TAIKHOAN_SELECT_FN', 'SELECT', 0);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_THANHVIEN_SELECT', 'VPD', 'THANHVIEN', 'Điều khiển truy cập danh sách thành viên nhóm', NULL, 'SELECT', 0);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, POLICY_FUNCTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('VPD_ATTACHMENT_SELECT', 'VPD', 'ATTACHMENT', 'Điều khiển truy cập file đính kèm theo MAC level', NULL, 'SELECT', 0);

-- FGA Policies bổ sung
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_NGUOIDUNG_UPDATE', 'FGA', 'NGUOIDUNG', 'Ghi nhật ký khi cập nhật thông tin người dùng', 'UPDATE', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_THANHVIEN_CHANGES', 'FGA', 'THANHVIEN', 'Ghi nhật ký khi thay đổi thành viên nhóm', 'INSERT,UPDATE,DELETE', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_CUOCTROCHUYEN_CREATE', 'FGA', 'CUOCTROCHUYEN', 'Ghi nhật ký khi tạo cuộc trò chuyện mới', 'INSERT', 0);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, STATEMENT_TYPES, IS_ENABLED)
VALUES('FGA_LOGIN_ATTEMPTS', 'FGA', 'TAIKHOAN', 'Ghi nhật ký các lần đăng nhập thất bại', 'UPDATE', 1);

-- DAC Policies bổ sung
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('DAC_ATTACHMENT_OWNER', 'DAC', 'ATTACHMENT', 'Chỉ chủ sở hữu mới được xóa file đính kèm', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('DAC_MESSAGE_EDIT_TIME', 'DAC', 'TINNHAN', 'Giới hạn thời gian chỉnh sửa tin nhắn (15 phút)', 0);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('DAC_GROUP_PRIVACY', 'DAC', 'CUOCTROCHUYEN', 'Bảo vệ cuộc trò chuyện riêng tư', 1);

-- MAC Policies bổ sung
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('MAC_READ_DOWN', 'MAC', 'TINNHAN', 'Cho phép đọc xuống mức thấp hơn (Read-Down)', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('MAC_NO_WRITE_UP', 'MAC', 'TINNHAN', 'Ngăn ghi lên mức cao hơn (No-Write-Up)', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('MAC_CLEARANCE_INHERIT', 'MAC', 'CUOCTROCHUYEN', 'Kế thừa mức bảo mật từ người tạo', 0);

-- RBAC Policies bổ sung
INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('RBAC_MODERATOR_MUTE', 'RBAC', 'THANHVIEN', 'Quyền Moderator: Mute thành viên', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('RBAC_ADMIN_BAN', 'RBAC', 'TAIKHOAN', 'Quyền Admin: Cấm tài khoản toàn cục', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('RBAC_OWNER_TRANSFER', 'RBAC', 'CUOCTROCHUYEN', 'Quyền Owner: Chuyển quyền sở hữu nhóm', 1);

INSERT INTO ADMIN_POLICY(POLICY_NAME, POLICY_TYPE, TABLE_NAME, DESCRIPTION, IS_ENABLED)
VALUES('RBAC_MEMBER_INVITE', 'RBAC', 'THANHVIEN', 'Quyền Member: Mời người khác vào nhóm', 0);

COMMIT;

--------------------------------------------------------------------------------
-- HOÀN TẤT POLICY
--------------------------------------------------------------------------------

COMMIT;

-- Verify
SELECT 'Policy configuration completed!' AS TRANG_THAI FROM DUAL;

SELECT POLICY_TYPE, COUNT(*) AS SO_LUONG 
FROM ADMIN_POLICY 
GROUP BY POLICY_TYPE 
ORDER BY POLICY_TYPE;

SELECT COUNT(*) AS SO_VPD_POLICY FROM USER_POLICIES;
SELECT COUNT(*) AS SO_TRIGGER FROM USER_TRIGGERS;

--------------------------------------------------------------------------------
-- KẾT THÚC POLICY.SQL
--------------------------------------------------------------------------------
