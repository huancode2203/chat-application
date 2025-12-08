--------------------------------------------------------------------------------
-- SCHEMA.SQL - CẤU TRÚC CƠ SỞ DỮ LIỆU CHO CHAT APPLICATION
-- Bao gồm: Tablespace, Profile, Session, Schema với các bảng cơ bản
-- 
-- HƯỚNG DẪN THỰC THI:
-- 1. Kết nối với SYS/SYSTEM (SYSDBA) để chạy PHẦN 1, 2
-- 2. Kết nối với ChatApplication để chạy PHẦN 3
--------------------------------------------------------------------------------

--------------------------------------------------------------------------------
-- PHẦN 1: TABLESPACE VÀ PROFILE (Chạy với SYS - SYSDBA)
--------------------------------------------------------------------------------

-- ============================================================================
-- 1.1) TẠO TABLESPACE
-- ============================================================================

-- Tablespace cho dữ liệu chính
CREATE TABLESPACE CHAT_DATA_TS
    DATAFILE 'chat_data01.dbf' SIZE 100M
    AUTOEXTEND ON NEXT 50M MAXSIZE 2G
    EXTENT MANAGEMENT LOCAL
    SEGMENT SPACE MANAGEMENT AUTO;

-- Tablespace cho dữ liệu tạm
CREATE TEMPORARY TABLESPACE CHAT_TEMP_TS
    TEMPFILE 'chat_temp01.dbf' SIZE 50M
    AUTOEXTEND ON NEXT 25M MAXSIZE 500M;

-- Tablespace cho index
CREATE TABLESPACE CHAT_INDEX_TS
    DATAFILE 'chat_index01.dbf' SIZE 50M
    AUTOEXTEND ON NEXT 25M MAXSIZE 1G
    EXTENT MANAGEMENT LOCAL;

-- Tablespace cho audit logs
CREATE TABLESPACE CHAT_AUDIT_TS
    DATAFILE 'chat_audit01.dbf' SIZE 100M
    AUTOEXTEND ON NEXT 50M MAXSIZE 5G
    EXTENT MANAGEMENT LOCAL;

-- ============================================================================
-- 1.2) TẠO PROFILE CHO NGƯỜI DÙNG
-- ============================================================================

-- Profile cho Quản trị viên (không giới hạn)
CREATE PROFILE CHAT_ADMIN_PROFILE LIMIT
    SESSIONS_PER_USER        UNLIMITED
    CPU_PER_SESSION          UNLIMITED
    CPU_PER_CALL             UNLIMITED
    CONNECT_TIME             UNLIMITED
    IDLE_TIME                60          -- Timeout sau 60 phút không hoạt động
    LOGICAL_READS_PER_SESSION UNLIMITED
    PRIVATE_SGA              UNLIMITED
    FAILED_LOGIN_ATTEMPTS    10          -- Khóa sau 10 lần đăng nhập sai
    PASSWORD_LIFE_TIME       180         -- Mật khẩu hết hạn sau 180 ngày
    PASSWORD_REUSE_TIME      365         -- Không dùng lại mật khẩu trong 1 năm
    PASSWORD_REUSE_MAX       12          -- Không dùng lại 12 mật khẩu gần nhất
    PASSWORD_LOCK_TIME       1/24        -- Khóa 1 giờ sau khi vượt số lần đăng nhập sai
    PASSWORD_GRACE_TIME      7;          -- 7 ngày để đổi mật khẩu sau khi hết hạn

-- Profile cho Người dùng thường (có giới hạn)
CREATE PROFILE CHAT_USER_PROFILE LIMIT
    SESSIONS_PER_USER        5           -- Tối đa 5 phiên đồng thời
    CPU_PER_SESSION          UNLIMITED
    CPU_PER_CALL             3000        -- Giới hạn CPU mỗi lệnh
    CONNECT_TIME             480         -- Tối đa 8 giờ kết nối
    IDLE_TIME                30          -- Timeout sau 30 phút không hoạt động
    LOGICAL_READS_PER_SESSION 100000     -- Giới hạn đọc logic
    PRIVATE_SGA              15M         -- Giới hạn bộ nhớ SGA
    FAILED_LOGIN_ATTEMPTS    5           -- Khóa sau 5 lần đăng nhập sai
    PASSWORD_LIFE_TIME       90          -- Mật khẩu hết hạn sau 90 ngày
    PASSWORD_REUSE_TIME      180         -- Không dùng lại mật khẩu trong 6 tháng
    PASSWORD_REUSE_MAX       6           -- Không dùng lại 6 mật khẩu gần nhất
    PASSWORD_LOCK_TIME       1/24        -- Khóa 1 giờ
    PASSWORD_GRACE_TIME      7;          -- 7 ngày để đổi mật khẩu

-- Profile cho Thực tập sinh (giới hạn nghiêm ngặt)
CREATE PROFILE CHAT_INTERN_PROFILE LIMIT
    SESSIONS_PER_USER        2           -- Tối đa 2 phiên
    CPU_PER_SESSION          UNLIMITED
    CPU_PER_CALL             1000        -- Giới hạn CPU thấp
    CONNECT_TIME             240         -- Tối đa 4 giờ
    IDLE_TIME                15          -- Timeout sau 15 phút
    LOGICAL_READS_PER_SESSION 50000      -- Giới hạn đọc logic
    PRIVATE_SGA              10M         -- Giới hạn bộ nhớ
    FAILED_LOGIN_ATTEMPTS    3           -- Khóa sau 3 lần đăng nhập sai
    PASSWORD_LIFE_TIME       30          -- Mật khẩu hết hạn sau 30 ngày
    PASSWORD_LOCK_TIME       1;          -- Khóa 1 ngày

-- ============================================================================
-- 1.3) TẠO USER VÀ CẤP QUYỀN
-- ============================================================================

-- Xóa user cũ nếu tồn tại
-- DROP USER ChatApplication CASCADE;

-- Tạo user chính với tablespace và profile
CREATE USER ChatApplication IDENTIFIED BY "Chat@App123"
    DEFAULT TABLESPACE CHAT_DATA_TS
    TEMPORARY TABLESPACE CHAT_TEMP_TS
    PROFILE CHAT_ADMIN_PROFILE
    QUOTA UNLIMITED ON CHAT_DATA_TS
    QUOTA UNLIMITED ON CHAT_INDEX_TS
    QUOTA UNLIMITED ON CHAT_AUDIT_TS;

-- Cấp quyền cơ bản
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE PROCEDURE,
      CREATE VIEW, CREATE TRIGGER, CREATE TYPE, CREATE SYNONYM
      TO ChatApplication;

-- Cấp quyền cho VPD, FGA và Context (bảo mật)
GRANT EXECUTE ON DBMS_RLS TO ChatApplication;
GRANT EXECUTE ON DBMS_FGA TO ChatApplication;
GRANT EXECUTE ON DBMS_SESSION TO ChatApplication;
GRANT EXECUTE ON DBMS_CRYPTO TO ChatApplication;

-- Cấp quyền EXEMPT ACCESS POLICY cho admin (bypass VPD khi cần)
-- GRANT EXEMPT ACCESS POLICY TO ChatApplication;

--------------------------------------------------------------------------------
-- PHẦN 2: TẠO CONTEXT (Chạy với SYS - SYSDBA)
--------------------------------------------------------------------------------

-- Context cho MAC (Mandatory Access Control)
CREATE OR REPLACE CONTEXT MAC_CTX USING ChatApplication.MAC_CTX_PKG;

-- Context cho Session Management
CREATE OR REPLACE CONTEXT SESSION_CTX USING ChatApplication.SESSION_CTX_PKG;

-- Context cho Admin Panel
CREATE OR REPLACE CONTEXT ADMIN_CTX USING ChatApplication.ADMIN_CTX_PKG;

--------------------------------------------------------------------------------
-- PHẦN 3: TẠO BẢNG (Chạy với ChatApplication)
--------------------------------------------------------------------------------

-- ============================================================================
-- 3.1) BẢNG VAI TRÒ TRONG HỆ THỐNG (RBAC)
-- ============================================================================
CREATE TABLE VAITRO (
    MAVAITRO    VARCHAR2(20) PRIMARY KEY,
    TENVAITRO   VARCHAR2(100) NOT NULL,
    CHUCNANG    VARCHAR2(500),
    CAPDO       NUMBER DEFAULT 1 CHECK (CAPDO BETWEEN 1 AND 10),
    NGAYTAO     TIMESTAMP DEFAULT SYSTIMESTAMP,
    MOTA        VARCHAR2(1000)
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.2) BẢNG TÀI KHOẢN NGƯỜI DÙNG
-- ============================================================================
CREATE TABLE TAIKHOAN (
    MATK            VARCHAR2(20) PRIMARY KEY,
    TENTK           VARCHAR2(100) NOT NULL UNIQUE,
    PASSWORD_HASH   VARCHAR2(256) NOT NULL,
    MAVAITRO        VARCHAR2(20),
    CLEARANCELEVEL  NUMBER DEFAULT 1 NOT NULL CHECK (CLEARANCELEVEL BETWEEN 1 AND 5),
    IS_BANNED_GLOBAL NUMBER(1) DEFAULT 0 CHECK (IS_BANNED_GLOBAL IN (0, 1)),
    IS_OTP_VERIFIED NUMBER(1) DEFAULT 0 CHECK (IS_OTP_VERIFIED IN (0, 1)),
    PROFILE_NAME    VARCHAR2(50) DEFAULT 'CHAT_USER_PROFILE',
    NGAYTAO         TIMESTAMP DEFAULT SYSTIMESTAMP,
    LAST_LOGIN      TIMESTAMP,
    LAST_LOGOUT     TIMESTAMP,
    LOGIN_COUNT     NUMBER DEFAULT 0,
    PUBLIC_KEY      CLOB,
    FAILED_LOGIN_ATTEMPTS NUMBER DEFAULT 0,      -- Số lần đăng nhập sai liên tiếp
    LOCKED_UNTIL    TIMESTAMP,                   -- Thời điểm hết khóa (NULL = không bị khóa)
    CONSTRAINT FK_TAIKHOAN_VAITRO FOREIGN KEY(MAVAITRO) REFERENCES VAITRO(MAVAITRO)
) TABLESPACE CHAT_DATA_TS;

CREATE INDEX IDX_TAIKHOAN_TENTK ON TAIKHOAN(TENTK) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_TAIKHOAN_MAVAITRO ON TAIKHOAN(MAVAITRO) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_TAIKHOAN_CLEARANCE ON TAIKHOAN(CLEARANCELEVEL) TABLESPACE CHAT_INDEX_TS;

-- ============================================================================
-- 3.3) BẢNG PHIÊN ĐĂNG NHẬP (SESSION MANAGEMENT)
-- ============================================================================
CREATE TABLE PHIENDANGNHAP (
    MAPHIEN         VARCHAR2(50) PRIMARY KEY,
    MATK            VARCHAR2(20) NOT NULL,
    IP_ADDRESS      VARCHAR2(50),
    USER_AGENT      VARCHAR2(500),
    THOIDIEM_DANGNHAP TIMESTAMP DEFAULT SYSTIMESTAMP,
    THOIDIEM_HETHAN TIMESTAMP,
    TRANG_THAI      VARCHAR2(20) DEFAULT 'ACTIVE' CHECK (TRANG_THAI IN ('ACTIVE', 'EXPIRED', 'LOGGED_OUT', 'FORCE_LOGOUT')),
    CLEARANCELEVEL_SESSION NUMBER DEFAULT 1,
    CONSTRAINT FK_PHIEN_TAIKHOAN FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE
) TABLESPACE CHAT_DATA_TS;

CREATE INDEX IDX_PHIEN_MATK ON PHIENDANGNHAP(MATK) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_PHIEN_TRANGTHAI ON PHIENDANGNHAP(TRANG_THAI) TABLESPACE CHAT_INDEX_TS;

-- ============================================================================
-- 3.4) BẢNG PHÒNG BAN
-- ============================================================================
CREATE TABLE PHONGBAN (
    MAPB        VARCHAR2(20) PRIMARY KEY,
    TENPB       VARCHAR2(200) NOT NULL,
    MOTA        VARCHAR2(500),
    CLEARANCELEVEL_MIN NUMBER DEFAULT 1 CHECK (CLEARANCELEVEL_MIN BETWEEN 1 AND 5),
    NGAYTAO     TIMESTAMP DEFAULT SYSTIMESTAMP
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.5) BẢNG CHỨC VỤ
-- ============================================================================
CREATE TABLE CHUCVU (
    MACV        VARCHAR2(20) PRIMARY KEY,
    TENCV       VARCHAR2(100) NOT NULL,
    CAPBAC      NUMBER DEFAULT 1 CHECK (CAPBAC BETWEEN 1 AND 10),
    CLEARANCELEVEL_DEFAULT NUMBER DEFAULT 1 CHECK (CLEARANCELEVEL_DEFAULT BETWEEN 1 AND 5),
    MOTA        VARCHAR2(500)
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.6) BẢNG THÔNG TIN NGƯỜI DÙNG CHI TIẾT
-- ============================================================================
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
) TABLESPACE CHAT_DATA_TS;

CREATE INDEX IDX_NGUOIDUNG_EMAIL ON NGUOIDUNG(EMAIL) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_NGUOIDUNG_MAPB ON NGUOIDUNG(MAPB) TABLESPACE CHAT_INDEX_TS;

-- ============================================================================
-- 3.7) BẢNG LOẠI CUỘC TRÒ CHUYỆN
-- ============================================================================
CREATE TABLE LOAICTC (
    MALOAICTC   VARCHAR2(20) PRIMARY KEY,
    TENLOAICTC  VARCHAR2(200) NOT NULL,
    IS_PRIVATE  VARCHAR2(1) DEFAULT 'N' CHECK (IS_PRIVATE IN ('Y', 'N')),
    MOTA        VARCHAR2(400)
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.8) BẢNG CUỘC TRÒ CHUYỆN
-- ============================================================================
CREATE TABLE CUOCTROCHUYEN (
    MACTC               VARCHAR2(60) PRIMARY KEY,
    MALOAICTC           VARCHAR2(20),
    TENCTC              VARCHAR2(200),
    NGAYTAO             TIMESTAMP DEFAULT SYSTIMESTAMP,
    NGUOIQL             VARCHAR2(20),
    IS_PRIVATE          VARCHAR2(1) DEFAULT 'N' CHECK (IS_PRIVATE IN ('Y', 'N')),
    CREATED_BY          VARCHAR2(20),
    AVATAR_URL          VARCHAR2(400),
    MOTA                VARCHAR2(1000),
    IS_ENCRYPTED        NUMBER(1) DEFAULT 0,
    IS_ARCHIVED         NUMBER(1) DEFAULT 0,
    ARCHIVED_AT         TIMESTAMP,
    MIN_CLEARANCE       NUMBER DEFAULT 1 CHECK (MIN_CLEARANCE BETWEEN 1 AND 5),
    THOIGIANTINNHANCUOI TIMESTAMP,  -- Thời gian tin nhắn cuối cùng
    CONSTRAINT FK_CTC_LOAICTC FOREIGN KEY(MALOAICTC) REFERENCES LOAICTC(MALOAICTC),
    CONSTRAINT FK_CTC_NGUOIQL FOREIGN KEY(NGUOIQL) REFERENCES TAIKHOAN(MATK)
) TABLESPACE CHAT_DATA_TS;

CREATE INDEX IDX_CTC_NGUOIQL ON CUOCTROCHUYEN(NGUOIQL) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_CTC_MALOAICTC ON CUOCTROCHUYEN(MALOAICTC) TABLESPACE CHAT_INDEX_TS;

-- ============================================================================
-- 3.9) BẢNG PHÂN QUYỀN TRONG NHÓM (RBAC cho nhóm chat)
-- ============================================================================
CREATE TABLE PHAN_QUYEN_NHOM (
    MAPHANQUYEN VARCHAR2(20) PRIMARY KEY,
    TENQUYEN    VARCHAR2(100) NOT NULL,
    CAN_ADD     NUMBER(1) DEFAULT 0,
    CAN_REMOVE  NUMBER(1) DEFAULT 0,
    CAN_PROMOTE NUMBER(1) DEFAULT 0,
    CAN_DELETE  NUMBER(1) DEFAULT 0,
    CAN_BAN     NUMBER(1) DEFAULT 0,
    CAN_UNBAN   NUMBER(1) DEFAULT 0,
    CAN_MUTE    NUMBER(1) DEFAULT 0,
    CAN_UNMUTE  NUMBER(1) DEFAULT 0,
    CAN_EDIT    NUMBER(1) DEFAULT 0,
    CAN_PIN     NUMBER(1) DEFAULT 0
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.10) BẢNG THÀNH VIÊN CUỘC TRÒ CHUYỆN
-- ============================================================================
CREATE TABLE THANHVIEN (
    MACTC             VARCHAR2(60),
    MATK              VARCHAR2(20),
    NGAYTHAMGIA       TIMESTAMP DEFAULT SYSTIMESTAMP,
    QUYEN             VARCHAR2(100) DEFAULT 'member',
    MAPHANQUYEN       VARCHAR2(20),
    IS_BANNED         NUMBER(1) DEFAULT 0,
    IS_MUTED          NUMBER(1) DEFAULT 0,
    DELETED_BY_MEMBER NUMBER(1) DEFAULT 0,
    NICKNAME          VARCHAR2(100),
    CONSTRAINT PK_THANHVIEN PRIMARY KEY(MACTC, MATK),
    CONSTRAINT FK_TV_CTC FOREIGN KEY(MACTC) REFERENCES CUOCTROCHUYEN(MACTC) ON DELETE CASCADE,
    CONSTRAINT FK_TV_TK FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE,
    CONSTRAINT FK_TV_PHANQUYEN FOREIGN KEY(MAPHANQUYEN) REFERENCES PHAN_QUYEN_NHOM(MAPHANQUYEN)
) TABLESPACE CHAT_DATA_TS;

CREATE INDEX IDX_TV_MATK ON THANHVIEN(MATK) TABLESPACE CHAT_INDEX_TS;

-- ============================================================================
-- 3.11) BẢNG LOẠI TIN NHẮN
-- ============================================================================
CREATE TABLE LOAITN (
    MALOAITN    VARCHAR2(20) PRIMARY KEY,
    TENLOAITN   VARCHAR2(100) NOT NULL
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.12) BẢNG TRẠNG THÁI TIN NHẮN
-- ============================================================================
CREATE TABLE TRANGTHAI (
    MATRANGTHAI     VARCHAR2(20) PRIMARY KEY,
    TENTRANGTHAI    VARCHAR2(100) NOT NULL
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.13) BẢNG TIN NHẮN (có SECURITYLABEL cho MAC)
-- ============================================================================
CREATE TABLE TINNHAN (
    MATN            NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    MACTC           VARCHAR2(60),
    MATK            VARCHAR2(20),
    MALOAITN        VARCHAR2(20),
    MATRANGTHAI     VARCHAR2(20),
    NOIDUNG         CLOB,
    NGAYGUI         TIMESTAMP DEFAULT SYSTIMESTAMP,
    SECURITYLABEL   NUMBER DEFAULT 1 NOT NULL CHECK (SECURITYLABEL BETWEEN 1 AND 5),
    -- Các cột hỗ trợ mã hóa
    IS_ENCRYPTED        NUMBER(1) DEFAULT 0,      -- 0=plaintext, 1=encrypted
    ENCRYPTED_CONTENT   RAW(2000),                -- Nội dung đã mã hóa (AES)
    ENCRYPTED_KEY       CLOB,                     -- Session key đã mã hóa (RSA) cho Hybrid
    ENCRYPTION_IV       VARCHAR2(100),            -- IV cho AES
    ENCRYPTION_TYPE     VARCHAR2(20) DEFAULT 'NONE', -- NONE, AES, RSA, HYBRID
    SIGNATURE           CLOB,                     -- Chữ ký số RSA
    IS_PINNED           NUMBER(1) DEFAULT 0,
    EDITED_AT           TIMESTAMP,
    CONSTRAINT FK_TN_CTC FOREIGN KEY(MACTC) REFERENCES CUOCTROCHUYEN(MACTC) ON DELETE CASCADE,
    CONSTRAINT FK_TN_TK FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK),
    CONSTRAINT FK_TN_LOAITN FOREIGN KEY(MALOAITN) REFERENCES LOAITN(MALOAITN),
    CONSTRAINT FK_TN_TRANGTHAI FOREIGN KEY(MATRANGTHAI) REFERENCES TRANGTHAI(MATRANGTHAI)
) TABLESPACE CHAT_DATA_TS;

CREATE INDEX IDX_TN_MACTC ON TINNHAN(MACTC) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_TN_MATK ON TINNHAN(MATK) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_TN_NGAYGUI ON TINNHAN(NGAYGUI) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_TN_SECURITYLABEL ON TINNHAN(SECURITYLABEL) TABLESPACE CHAT_INDEX_TS;

-- ============================================================================
-- 3.14) BẢNG TỆP ĐÍNH KÈM
-- ============================================================================
CREATE TABLE ATTACHMENT (
    ATTACH_ID       NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    MATK            VARCHAR2(20) NOT NULL,
    FILENAME        VARCHAR2(255) NOT NULL,
    MIMETYPE        VARCHAR2(200),
    FILESIZE        NUMBER,
    FILEDATA        BLOB,
    STORAGE_URL     VARCHAR2(400),
    UPLOADED_AT     TIMESTAMP DEFAULT SYSTIMESTAMP,
    -- Các cột hỗ trợ mã hóa Hybrid (AES + RSA)
    IS_ENCRYPTED        NUMBER(1) DEFAULT 0,      -- 0=plaintext, 1=encrypted
    ENCRYPTED_CONTENT   BLOB,                     -- File đã mã hóa bằng AES
    ENCRYPTION_KEY      RAW(256),                 -- AES key (32 bytes)
    ENCRYPTION_IV       RAW(16),                  -- AES IV (16 bytes)
    ENCRYPTION_TYPE     VARCHAR2(20) DEFAULT 'NONE', -- NONE, AES, HYBRID
    CONSTRAINT FK_ATTACH_TK FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.15) BẢNG LIÊN KẾT TIN NHẮN - ĐÍNH KÈM
-- ============================================================================
CREATE TABLE TINNHAN_ATTACH (
    MATN        NUMBER,
    ATTACH_ID   NUMBER,
    CONSTRAINT PK_TN_ATTACH PRIMARY KEY(MATN, ATTACH_ID),
    CONSTRAINT FK_TNA_MATN FOREIGN KEY(MATN) REFERENCES TINNHAN(MATN) ON DELETE CASCADE,
    CONSTRAINT FK_TNA_ATTACH FOREIGN KEY(ATTACH_ID) REFERENCES ATTACHMENT(ATTACH_ID) ON DELETE CASCADE
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.16) BẢNG XÁC THỰC OTP
-- ============================================================================
CREATE TABLE XACTHUCOTP (
    MAOTP           NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    MATK            VARCHAR2(20),
    EMAIL           VARCHAR2(200),
    MAXTOTP         VARCHAR2(256),
    THOIGIANTAO     TIMESTAMP DEFAULT SYSTIMESTAMP,
    THOIGIANTONTAI  TIMESTAMP,
    DAXACMINH       NUMBER(1) DEFAULT 0,
    LOAI            VARCHAR2(50) DEFAULT 'REGISTER',
    CONSTRAINT FK_OTP_TK FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.17) BẢNG AUDIT LOGS (Ghi nhật ký hệ thống)
-- ============================================================================
CREATE TABLE AUDIT_LOGS (
    LOG_ID          NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    MATK            VARCHAR2(20),
    ACTION          VARCHAR2(200) NOT NULL,
    TARGET          VARCHAR2(500),
    SECURITYLABEL   NUMBER DEFAULT 0,
    TIMESTAMP       TIMESTAMP DEFAULT SYSTIMESTAMP,
    IP_ADDRESS      VARCHAR2(50),
    USER_AGENT      VARCHAR2(500),
    DETAILS         CLOB,
    STATUS          VARCHAR2(50) DEFAULT 'SUCCESS',
    SESSION_ID      VARCHAR2(50),
    OLD_VALUE       CLOB,
    NEW_VALUE       CLOB
) TABLESPACE CHAT_AUDIT_TS;

CREATE INDEX IDX_AUDIT_TIMESTAMP ON AUDIT_LOGS(TIMESTAMP) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_AUDIT_MATK ON AUDIT_LOGS(MATK) TABLESPACE CHAT_INDEX_TS;
CREATE INDEX IDX_AUDIT_ACTION ON AUDIT_LOGS(ACTION) TABLESPACE CHAT_INDEX_TS;

-- ============================================================================
-- 3.18) BẢNG QUẢN LÝ KHÓA MÃ HÓA
-- ============================================================================
CREATE TABLE ENCRYPTION_KEYS (
    KEY_ID          NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    MATK            VARCHAR2(20),
    KEY_TYPE        VARCHAR2(20) NOT NULL,
    KEY_VALUE       CLOB NOT NULL,
    CREATED_AT      TIMESTAMP DEFAULT SYSTIMESTAMP,
    EXPIRES_AT      TIMESTAMP,
    IS_ACTIVE       NUMBER(1) DEFAULT 1,
    CONSTRAINT FK_ENCKEY_TK FOREIGN KEY(MATK) REFERENCES TAIKHOAN(MATK) ON DELETE CASCADE
) TABLESPACE CHAT_DATA_TS;

CREATE INDEX IDX_ENCKEY_MATK ON ENCRYPTION_KEYS(MATK) TABLESPACE CHAT_INDEX_TS;

-- ============================================================================
-- 3.19) BẢNG POLICY ADMIN PANEL (Quản lý chính sách bảo mật)
-- ============================================================================
CREATE TABLE ADMIN_POLICY (
    POLICY_ID       NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    POLICY_NAME     VARCHAR2(100) NOT NULL UNIQUE,
    POLICY_TYPE     VARCHAR2(50) NOT NULL CHECK (POLICY_TYPE IN ('VPD', 'FGA', 'DAC', 'MAC', 'RBAC', 'OLS')),
    TABLE_NAME      VARCHAR2(100) NOT NULL,
    DESCRIPTION     VARCHAR2(1000),
    POLICY_FUNCTION VARCHAR2(200),
    STATEMENT_TYPES VARCHAR2(100),
    IS_ENABLED      NUMBER(1) DEFAULT 1,
    CREATED_BY      VARCHAR2(20),
    CREATED_AT      TIMESTAMP DEFAULT SYSTIMESTAMP,
    UPDATED_AT      TIMESTAMP,
    CONSTRAINT FK_POLICY_TK FOREIGN KEY(CREATED_BY) REFERENCES TAIKHOAN(MATK)
) TABLESPACE CHAT_DATA_TS;

-- ============================================================================
-- 3.20) BẢNG LOG THAY ĐỔI POLICY
-- ============================================================================
CREATE TABLE POLICY_CHANGE_LOG (
    LOG_ID          NUMBER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    POLICY_ID       NUMBER,
    ACTION          VARCHAR2(50) NOT NULL CHECK (ACTION IN ('CREATE', 'UPDATE', 'DELETE', 'ENABLE', 'DISABLE')),
    CHANGED_BY      VARCHAR2(20),
    CHANGED_AT      TIMESTAMP DEFAULT SYSTIMESTAMP,
    OLD_VALUE       CLOB,
    NEW_VALUE       CLOB,
    REASON          VARCHAR2(500),
    CONSTRAINT FK_PCL_POLICY FOREIGN KEY(POLICY_ID) REFERENCES ADMIN_POLICY(POLICY_ID),
    CONSTRAINT FK_PCL_TK FOREIGN KEY(CHANGED_BY) REFERENCES TAIKHOAN(MATK)
) TABLESPACE CHAT_AUDIT_TS;

--------------------------------------------------------------------------------
-- HOÀN TẤT SCHEMA
--------------------------------------------------------------------------------

COMMIT;

-- Verify
SELECT 'Schema created successfully!' AS TRANG_THAI FROM DUAL;
SELECT COUNT(*) AS SO_BANG FROM USER_TABLES;

--------------------------------------------------------------------------------
-- KẾT THÚC SCHEMA.SQL
--------------------------------------------------------------------------------
