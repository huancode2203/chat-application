--------------------------------------------------------------------------------
-- 01_SYS_SETUP.SQL - CHẠY VỚI SYS AS SYSDBA
-- Bao gồm: Tablespace, Profile, User, Context, Roles cơ bản
--------------------------------------------------------------------------------
SET SERVEROUTPUT ON;

-- =============================================================================
-- 1. TABLESPACE - Không gian lưu trữ (bỏ qua nếu đã có)
-- =============================================================================
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_tablespaces WHERE tablespace_name = 'CHAT_DATA_TS';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLESPACE CHAT_DATA_TS DATAFILE ''chat_data01.dbf'' SIZE 100M AUTOEXTEND ON NEXT 50M MAXSIZE 2G EXTENT MANAGEMENT LOCAL';
        DBMS_OUTPUT.PUT_LINE('Created CHAT_DATA_TS');
    ELSE
        DBMS_OUTPUT.PUT_LINE('CHAT_DATA_TS already exists - skipped');
    END IF;
END;
/

DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_tablespaces WHERE tablespace_name = 'CHAT_AUDIT_TS';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE TABLESPACE CHAT_AUDIT_TS DATAFILE ''chat_audit01.dbf'' SIZE 50M AUTOEXTEND ON NEXT 25M MAXSIZE 1G EXTENT MANAGEMENT LOCAL';
        DBMS_OUTPUT.PUT_LINE('Created CHAT_AUDIT_TS');
    ELSE
        DBMS_OUTPUT.PUT_LINE('CHAT_AUDIT_TS already exists - skipped');
    END IF;
END;
/

-- =============================================================================
-- 2. PROFILE - Giới hạn session và tài nguyên (bỏ qua nếu đã có)
-- =============================================================================
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_profiles WHERE profile = 'CHAT_ADMIN_PROFILE' AND ROWNUM = 1;
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE PROFILE CHAT_ADMIN_PROFILE LIMIT 
        SESSIONS_PER_USER 5 
        CPU_PER_SESSION UNLIMITED 
        CPU_PER_CALL 60000 
        CONNECT_TIME 480 
        IDLE_TIME 60 
        FAILED_LOGIN_ATTEMPTS 5 
        PASSWORD_LIFE_TIME 90 
        PASSWORD_REUSE_TIME 365 
        PASSWORD_REUSE_MAX 10 
        PASSWORD_LOCK_TIME 1/24';
    END IF;
END;
/

DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_profiles WHERE profile = 'CHAT_USER_PROFILE' AND ROWNUM = 1;
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE PROFILE CHAT_USER_PROFILE LIMIT 
        SESSIONS_PER_USER 3 
        CPU_PER_SESSION DEFAULT 
        CPU_PER_CALL 30000 
        CONNECT_TIME 240 
        IDLE_TIME 30 
        FAILED_LOGIN_ATTEMPTS 5 
        PASSWORD_LIFE_TIME 180 
        PASSWORD_LOCK_TIME 1/48';
    END IF;
END;
/

DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_profiles WHERE profile = 'CHAT_INTERN_PROFILE' AND ROWNUM = 1;
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE PROFILE CHAT_INTERN_PROFILE LIMIT 
        SESSIONS_PER_USER 2 
        CPU_PER_SESSION DEFAULT 
        CPU_PER_CALL 10000 
        CONNECT_TIME 120 
        IDLE_TIME 15 
        FAILED_LOGIN_ATTEMPTS 3 
        PASSWORD_LIFE_TIME 30 
        PASSWORD_LOCK_TIME 1/24';
    END IF;
END;
/

-- =============================================================================
-- 3. USER - Tạo user ChatApplication
-- =============================================================================
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_users WHERE username = 'CHATAPPLICATION';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE USER ChatApplication IDENTIFIED BY "123" DEFAULT TABLESPACE CHAT_DATA_TS 
        QUOTA UNLIMITED ON CHAT_DATA_TS 
        QUOTA UNLIMITED ON CHAT_AUDIT_TS 
        PROFILE CHAT_ADMIN_PROFILE';
        DBMS_OUTPUT.PUT_LINE('Created user ChatApplication');
    ELSE
        DBMS_OUTPUT.PUT_LINE('User ChatApplication already exists - skipped');
    END IF;
END;
/

-- Quyền cơ bản
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE TO ChatApplication;
GRANT CREATE PROCEDURE, CREATE VIEW, CREATE TRIGGER TO ChatApplication;

-- Quyền cho VPD, FGA, Encryption
GRANT EXECUTE ON DBMS_RLS TO ChatApplication;
GRANT EXECUTE ON DBMS_FGA TO ChatApplication;
GRANT EXECUTE ON DBMS_SESSION TO ChatApplication;
GRANT EXECUTE ON DBMS_CRYPTO TO ChatApplication;

-- Quyền Audit
GRANT AUDIT ANY TO ChatApplication;
GRANT AUDIT SYSTEM TO ChatApplication;

-- =============================================================================
-- 4. CONTEXT - Cho MAC và Session
-- =============================================================================
CREATE OR REPLACE CONTEXT MAC_CTX USING ChatApplication.MAC_CTX_PKG;
CREATE OR REPLACE CONTEXT SESSION_CTX USING ChatApplication.SESSION_CTX_PKG;
CREATE OR REPLACE CONTEXT ADMIN_CTX USING ChatApplication.ADMIN_CTX_PKG;

-- =============================================================================
-- 5. DAC - ROLES CƠ BẢN (bỏ qua nếu đã có)
-- LƯU Ý: Grants trên tables sẽ chạy sau trong file 06_grants.sql
-- =============================================================================
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_roles WHERE role = 'CHAT_OWNER_ROLE';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE ROLE CHAT_OWNER_ROLE';
        EXECUTE IMMEDIATE 'GRANT ALL PRIVILEGES TO CHAT_OWNER_ROLE';
    END IF;
END;
/

DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_roles WHERE role = 'CHAT_ADMIN_ROLE';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE ROLE CHAT_ADMIN_ROLE';
    END IF;
    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO CHAT_ADMIN_ROLE';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_roles WHERE role = 'CHAT_USER_ROLE';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE ROLE CHAT_USER_ROLE';
    END IF;
    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO CHAT_USER_ROLE';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count FROM dba_roles WHERE role = 'CHAT_INTERN_ROLE';
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE 'CREATE ROLE CHAT_INTERN_ROLE';
    END IF;
    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO CHAT_INTERN_ROLE';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- =============================================================================
-- 6. GÁN ROLE CHO USER ChatApplication
-- =============================================================================
BEGIN EXECUTE IMMEDIATE 'GRANT CHAT_OWNER_ROLE TO ChatApplication'; EXCEPTION WHEN OTHERS THEN NULL; END;
/
BEGIN EXECUTE IMMEDIATE 'GRANT DBA TO ChatApplication'; EXCEPTION WHEN OTHERS THEN NULL; END;
/

COMMIT;

SELECT 'SYS Setup completed!' AS STATUS FROM DUAL;
