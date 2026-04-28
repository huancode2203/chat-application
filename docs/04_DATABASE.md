# PHẦN 4: ORACLE DATABASE SECURITY

## 4.1 Schema Database

### 4.1.1 ERD (Entity Relationship Diagram)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DATABASE SCHEMA                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌──────────────┐       ┌──────────────┐       ┌──────────────┐           │
│   │   VAITRO     │       │   CHUCVU     │       │   PHONGBAN   │           │
│   ├──────────────┤       ├──────────────┤       ├──────────────┤           │
│   │ MAVAITRO (PK)│       │ MACV (PK)    │       │ MAPB (PK)    │           │
│   │ TENVAITRO    │       │ TENCV        │       │ TENPB        │           │
│   └──────┬───────┘       └──────┬───────┘       └──────┬───────┘           │
│          │                      │                      │                    │
│          │                      └──────────┬───────────┘                    │
│          │                                 │                                │
│          ▼                                 ▼                                │
│   ┌──────────────┐               ┌──────────────┐                          │
│   │  TAIKHOAN    │◄──────────────│  NGUOIDUNG   │                          │
│   ├──────────────┤               ├──────────────┤                          │
│   │ MATK (PK)    │               │ MATK (PK,FK) │                          │
│   │ TENTK        │               │ HOVATEN      │                          │
│   │ PASSWORD_HASH│               │ EMAIL        │                          │
│   │ MAVAITRO (FK)│               │ SDT          │                          │
│   │ CLEARANCELEVEL│              │ MACV (FK)    │                          │
│   │ IS_BANNED    │               │ MAPB (FK)    │                          │
│   │ NGAYTAO      │               │ DIACHI       │                          │
│   └──────┬───────┘               └──────────────┘                          │
│          │                                                                  │
│          │                                                                  │
│          ▼                                                                  │
│   ┌──────────────┐               ┌──────────────┐                          │
│   │  XACTHUCOTP  │               │  AUDIT_LOGS  │                          │
│   ├──────────────┤               ├──────────────┤                          │
│   │ MAOTP (PK)   │               │ LOG_ID (PK)  │                          │
│   │ MATK (FK)    │               │ MATK (FK)    │                          │
│   │ OTP_HASH     │               │ ACTION       │                          │
│   │ HETHAN       │               │ TARGET       │                          │
│   │ DAXACMINH    │               │ TIMESTAMP    │                          │
│   └──────────────┘               │ SECURITYLABEL│                          │
│                                  └──────────────┘                          │
│                                                                             │
│   ┌──────────────┐       ┌──────────────┐       ┌──────────────┐           │
│   │CUOCTROCHUYEN │◄──────│  THANHVIEN   │───────│  TINNHAN     │           │
│   ├──────────────┤       ├──────────────┤       ├──────────────┤           │
│   │ MACTC (PK)   │       │ MACTC (PK,FK)│       │ MATN (PK)    │           │
│   │ TENCTC       │       │ MATK (PK,FK) │       │ MACTC (FK)   │           │
│   │ MALOAICTC    │       │ QUYEN        │       │ MATK (FK)    │           │
│   │ IS_PRIVATE   │       │ NGAYTHAMGIA  │       │ NOIDUNG      │           │
│   │ MIN_CLEARANCE│       └──────────────┘       │ SECURITYLABEL│           │
│   │ NGUOIQL      │                              │ THOIGIAN     │           │
│   └──────────────┘                              └──────────────┘           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.1.2 Bảng TAIKHOAN

**File: ChatServer/Database/02_schema.sql**

```sql
CREATE TABLE TAIKHOAN (
    MATK                VARCHAR2(20) PRIMARY KEY,
    TENTK               VARCHAR2(50) UNIQUE NOT NULL,
    PASSWORD_HASH       VARCHAR2(128) NOT NULL,
    MAVAITRO            VARCHAR2(10) DEFAULT 'VT003' REFERENCES VAITRO(MAVAITRO),
    CLEARANCELEVEL      NUMBER(1) DEFAULT 1 CHECK (CLEARANCELEVEL BETWEEN 1 AND 5),
    IS_BANNED_GLOBAL    NUMBER(1) DEFAULT 0,
    NGAYTAO             TIMESTAMP DEFAULT SYSTIMESTAMP,
    FAILED_LOGIN_ATTEMPTS NUMBER(2) DEFAULT 0,
    LOCKED_UNTIL        TIMESTAMP NULL
);

-- Index để tìm kiếm nhanh
CREATE INDEX IDX_TAIKHOAN_TENTK ON TAIKHOAN(TENTK);
```

### 4.1.3 Bảng TINNHAN với Security Label

```sql
CREATE TABLE TINNHAN (
    MATN            NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    MACTC           VARCHAR2(50) NOT NULL REFERENCES CUOCTROCHUYEN(MACTC),
    MATK            VARCHAR2(20) NOT NULL REFERENCES TAIKHOAN(MATK),
    NOIDUNG         CLOB,
    THOIGIAN        TIMESTAMP DEFAULT SYSTIMESTAMP,
    SECURITYLABEL   NUMBER(1) DEFAULT 1 CHECK (SECURITYLABEL BETWEEN 1 AND 5),
    MALOAITN        VARCHAR2(20) DEFAULT 'TEXT',
    MATRANGTHAI     VARCHAR2(20) DEFAULT 'ACTIVE'
);

-- Index cho VPD filter
CREATE INDEX IDX_TINNHAN_SECURITY ON TINNHAN(SECURITYLABEL);
CREATE INDEX IDX_TINNHAN_MACTC ON TINNHAN(MACTC);
```

---

## 4.2 VPD (Virtual Private Database)

### 4.2.1 Khái niệm

VPD tự động thêm WHERE clause vào mọi query, đảm bảo user chỉ thấy dữ liệu được phép.

```
User Query:          SELECT * FROM TINNHAN WHERE MACTC = 'CTC001'
                                    │
                                    ▼
VPD Transform:       SELECT * FROM TINNHAN WHERE MACTC = 'CTC001'
                     AND SECURITYLABEL <= 2  -- Tự động thêm bởi VPD
```

### 4.2.2 Bell-LaPadula Model

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      BELL-LAPADULA SECURITY MODEL                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Clearance Levels:                                                         │
│                                                                             │
│   Level 5 ─────────────────────────────────────────────── Giám đốc         │
│      │    Đọc: Level 1-5                                                    │
│      │    Viết: Level 5                                                     │
│      │                                                                      │
│   Level 4 ─────────────────────────────────────────────── Trưởng phòng     │
│      │    Đọc: Level 1-4                                                    │
│      │    Viết: Level 4-5                                                   │
│      │                                                                      │
│   Level 3 ─────────────────────────────────────────────── Nhân viên        │
│      │    Đọc: Level 1-3                                                    │
│      │    Viết: Level 3-5                                                   │
│      │                                                                      │
│   Level 2 ─────────────────────────────────────────────── Nhân viên mới    │
│      │    Đọc: Level 1-2                                                    │
│      │    Viết: Level 2-5                                                   │
│      │                                                                      │
│   Level 1 ─────────────────────────────────────────────── Thực tập sinh    │
│           Đọc: Level 1 only                                                 │
│           Viết: Level 1-5                                                   │
│                                                                             │
│   Rules:                                                                    │
│   - NO READ UP: Không đọc dữ liệu level cao hơn mình                       │
│   - NO WRITE DOWN: Không viết dữ liệu level thấp hơn mình                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2.3 VPD Policy Function

**File: ChatServer/Database/04_policies.sql**

```sql
-- Policy function cho READ (No Read Up)
CREATE OR REPLACE FUNCTION VPD_TINNHAN_READ(
    p_schema VARCHAR2,
    p_obj VARCHAR2
) RETURN VARCHAR2 AS
    v_level NUMBER;
BEGIN
    -- Lấy clearance level từ MAC Context
    v_level := NVL(SYS_CONTEXT('MAC_CTX', 'CLEARANCE_LEVEL'), 1);

    -- Trả về predicate: chỉ đọc tin nhắn có level <= user level
    RETURN 'SECURITYLABEL <= ' || v_level;
END;
/

-- Policy function cho WRITE (No Write Down)
CREATE OR REPLACE FUNCTION VPD_TINNHAN_WRITE(
    p_schema VARCHAR2,
    p_obj VARCHAR2
) RETURN VARCHAR2 AS
    v_level NUMBER;
BEGIN
    v_level := NVL(SYS_CONTEXT('MAC_CTX', 'CLEARANCE_LEVEL'), 1);

    -- Trả về predicate: chỉ viết tin nhắn có level >= user level
    RETURN 'SECURITYLABEL >= ' || v_level;
END;
/

-- Đăng ký policies
BEGIN
    -- READ policy
    DBMS_RLS.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_READ_POLICY',
        function_schema => USER,
        policy_function => 'VPD_TINNHAN_READ',
        statement_types => 'SELECT'
    );

    -- WRITE policy
    DBMS_RLS.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'TINNHAN',
        policy_name     => 'VPD_TINNHAN_WRITE_POLICY',
        function_schema => USER,
        policy_function => 'VPD_TINNHAN_WRITE',
        statement_types => 'INSERT,UPDATE,DELETE'
    );
END;
/
```

### 4.2.4 Set MAC Context từ C#

**File: ChatServer/Database/DbContext.cs (Line 131-152)**

```csharp
/// <summary>
/// Set MAC Context cho VPD policies
/// Phải gọi SAU KHI user login thành công
/// </summary>
public async Task SetMacContextAsync(string matk, int clearanceLevel)
{
    using var cmd = Connection.CreateCommand();
    cmd.CommandText = "BEGIN SET_MAC_CONTEXT(:p_matk, :p_level); END;";
    cmd.CommandType = CommandType.Text;

    cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
    cmd.Parameters.Add(new OracleParameter("p_level", OracleDbType.Int32) { Value = clearanceLevel });

    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"[VPD] Set context: MATK={matk}, Level={clearanceLevel}");
}
```

**Stored Procedure:**

```sql
-- File: ChatServer/Database/03_procedures.sql
CREATE OR REPLACE PROCEDURE SET_MAC_CONTEXT(
    p_matk VARCHAR2,
    p_level NUMBER DEFAULT NULL
) AS
    v_level NUMBER;
BEGIN
    IF p_level IS NOT NULL THEN
        v_level := p_level;
    ELSE
        -- Lấy từ database nếu không truyền vào
        SELECT CLEARANCELEVEL INTO v_level
        FROM TAIKHOAN
        WHERE MATK = p_matk;
    END IF;

    -- Set context values
    DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'USERNAME', p_matk);
    DBMS_SESSION.SET_CONTEXT('MAC_CTX', 'CLEARANCE_LEVEL', TO_CHAR(v_level));
END;
/
```

---

## 4.3 FGA (Fine-Grained Auditing)

### 4.3.1 Khái niệm

FGA tự động ghi log khi có truy cập vào dữ liệu nhạy cảm.

### 4.3.2 Tạo FGA Policy

**File: ChatServer/Database/04_policies.sql**

```sql
-- FGA cho tin nhắn có security label >= 3 (nhạy cảm)
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'TINNHAN',
        policy_name     => 'FGA_TINNHAN_SENSITIVE',
        audit_condition => 'SECURITYLABEL >= 3',  -- Chỉ audit level 3+
        audit_column    => 'NOIDUNG',             -- Column được audit
        statement_types => 'SELECT,INSERT,UPDATE,DELETE',
        enable          => TRUE
    );
END;
/

-- FGA cho bảng TAIKHOAN (audit mọi truy cập password)
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => USER,
        object_name     => 'TAIKHOAN',
        policy_name     => 'FGA_TAIKHOAN_PASSWORD',
        audit_column    => 'PASSWORD_HASH',
        statement_types => 'SELECT,UPDATE',
        enable          => TRUE
    );
END;
/
```

### 4.3.3 Xem FGA Logs

```sql
-- Query FGA audit trail
SELECT TIMESTAMP, DB_USER, OBJECT_NAME, POLICY_NAME, SQL_TEXT
FROM DBA_FGA_AUDIT_TRAIL
WHERE OBJECT_NAME = 'TINNHAN'
ORDER BY TIMESTAMP DESC;
```

### 4.3.4 Load FGA từ C#

**File: ChatServer/Forms/PolicyManagementForm.cs (Line 444-502)**

```csharp
private async Task LoadFGAAsync()
{
    try
    {
        lblStatus.Text = "Loading FGA...";
        using var cmd = _dbContext.Connection.CreateCommand();
        var list = new List<object>();

        try
        {
            // Thử query DBA_AUDIT_POLICIES (cần quyền)
            cmd.CommandText = @"
                SELECT OBJECT_NAME, POLICY_NAME, ENABLED,
                       NVL(AUDIT_COLUMN,'ALL') AS AUDIT_COL,
                       NVL(STATEMENT_TYPES,'SELECT') AS STMT_TYPES
                FROM DBA_AUDIT_POLICIES
                WHERE OBJECT_SCHEMA = USER
                ORDER BY OBJECT_NAME";

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new
                {
                    Table = r.GetString(0),
                    Policy = r.GetString(1),
                    Status = r.GetString(2) == "YES" ? "Enabled" : "Disabled",
                    Column = r.GetString(3),
                    Statements = r.GetString(4)
                });
            }
        }
        catch
        {
            // Fallback nếu không có quyền DBA
            list.Clear();
            using var cmd2 = _dbContext.Connection.CreateCommand();
            cmd2.CommandText = @"
                SELECT TABLE_NAME, POLICY_NAME, IS_ENABLED, STATEMENT_TYPES
                FROM ADMIN_POLICY
                WHERE POLICY_TYPE = 'FGA'
                ORDER BY TABLE_NAME";

            using var r2 = await cmd2.ExecuteReaderAsync();
            while (await r2.ReadAsync())
            {
                list.Add(new
                {
                    Table = r2.GetString(0),
                    Policy = r2.GetString(1),
                    Status = r2.GetInt32(2) == 1 ? "Enabled" : "Disabled",
                    Column = "ALL",
                    Statements = r2.GetString(3)
                });
            }
        }

        dgvFGA.DataSource = list;
        lblStatus.Text = $"FGA: {list.Count} policies";
    }
    catch (Exception ex)
    {
        lblStatus.Text = $"Error: {ex.Message}";
    }
}
```

---

## 4.4 Stored Procedures

### 4.4.1 SP_CAPNHAT_NGUOIDUNG_ADMIN

**File: ChatServer/Database/03_procedures.sql (Line 364-403)**

```sql
CREATE OR REPLACE PROCEDURE SP_CAPNHAT_NGUOIDUNG_ADMIN(
    p_matk      VARCHAR2,
    p_email     VARCHAR2 DEFAULT NULL,
    p_hovaten   VARCHAR2 DEFAULT NULL,
    p_sdt       VARCHAR2 DEFAULT NULL,
    p_diachi    VARCHAR2 DEFAULT NULL,
    p_bio       VARCHAR2 DEFAULT NULL,
    p_macv      VARCHAR2 DEFAULT NULL,  -- Chức vụ
    p_mapb      VARCHAR2 DEFAULT NULL   -- Phòng ban
) AS
BEGIN
    -- MERGE: Update nếu tồn tại, Insert nếu chưa có
    MERGE INTO NGUOIDUNG n
    USING (SELECT p_matk AS MATK FROM DUAL) d
    ON (n.MATK = d.MATK)

    WHEN MATCHED THEN
        UPDATE SET
            EMAIL = NVL(p_email, n.EMAIL),
            HOVATEN = NVL(p_hovaten, n.HOVATEN),
            SDT = NVL(p_sdt, n.SDT),
            DIACHI = NVL(p_diachi, n.DIACHI),
            BIO = NVL(p_bio, n.BIO),
            MACV = NVL(p_macv, n.MACV),
            MAPB = NVL(p_mapb, n.MAPB)

    WHEN NOT MATCHED THEN
        INSERT (MATK, EMAIL, HOVATEN, SDT, MACV, MAPB)
        VALUES (p_matk, p_email, p_hovaten, p_sdt,
                NVL(p_macv, 'CV005'),  -- Mặc định: Thực tập sinh
                p_mapb);

    -- Audit log
    INSERT INTO AUDIT_LOGS(MATK, ACTION, TARGET)
    VALUES(MAC_CTX_PKG.GET_USERNAME, 'UPDATE_USER_INFO', p_matk);

    COMMIT;
END;
/
```

### 4.4.2 Gọi từ C#

**File: ChatServer/Database/DbContext.cs (Line 823-862)**

```csharp
public async Task UpdateUserInfoAsync(
    string username,
    string? email = null,
    string? hovaten = null,
    string? sdt = null,
    string? diachi = null,
    string? bio = null,
    string? macv = null,
    string? mapb = null)
{
    // Tìm MATK từ username
    var matk = await GetMatkFromUsernameAsync(username);
    if (string.IsNullOrEmpty(matk)) return;

    using var cmd = Connection.CreateCommand();
    cmd.CommandText = "SP_CAPNHAT_NGUOIDUNG_ADMIN";
    cmd.CommandType = CommandType.StoredProcedure;

    cmd.Parameters.Add(new OracleParameter("p_matk", OracleDbType.Varchar2) { Value = matk });
    cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Value = (object?)email ?? DBNull.Value });
    cmd.Parameters.Add(new OracleParameter("p_hovaten", OracleDbType.Varchar2) { Value = (object?)hovaten ?? DBNull.Value });
    cmd.Parameters.Add(new OracleParameter("p_sdt", OracleDbType.Varchar2) { Value = (object?)sdt ?? DBNull.Value });
    cmd.Parameters.Add(new OracleParameter("p_diachi", OracleDbType.Varchar2) { Value = (object?)diachi ?? DBNull.Value });
    cmd.Parameters.Add(new OracleParameter("p_bio", OracleDbType.Varchar2) { Value = (object?)bio ?? DBNull.Value });
    cmd.Parameters.Add(new OracleParameter("p_macv", OracleDbType.Varchar2) { Value = (object?)macv ?? DBNull.Value });
    cmd.Parameters.Add(new OracleParameter("p_mapb", OracleDbType.Varchar2) { Value = (object?)mapb ?? DBNull.Value });

    await cmd.ExecuteNonQueryAsync();
}
```

---

## 4.5 Quản lý Policies từ Admin Panel

### 4.5.1 PolicyManagementForm

**File: ChatServer/Forms/PolicyManagementForm.cs**

```csharp
public partial class PolicyManagementForm : Form
{
    private readonly DbContext _dbContext;
    private readonly string _adminUsername;

    public PolicyManagementForm(DbContext dbContext, string adminUsername)
    {
        _dbContext = dbContext;
        _adminUsername = adminUsername;
        InitializeComponent();

        // Tabs: VPD, FGA, MAC
        tabControl.SelectedIndexChanged += async (s, e) => await LoadCurrentTabAsync();
    }

    private async Task LoadCurrentTabAsync()
    {
        switch (tabControl.SelectedIndex)
        {
            case 0: await LoadVPDAsync(); break;
            case 1: await LoadFGAAsync(); break;
            case 2: await LoadMACAsync(); break;
        }
    }
}
```

---

_Tiếp theo: 05_SERVER.md - Server-side Components_
