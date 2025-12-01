--------------------------------------------------------------------------------
-- seeds.sql
-- Run AFTER schema.sql (connected as CHATNOIBO_DOAN1).
-- This file contains all INSERTs (dữ liệu mẫu) — separated per your request.
--------------------------------------------------------------------------------

-- Nhập liệu VAITRO (theo pattern bạn cung cấp)
--Lưu ý: dùng N'...' để hỗ trợ unicode nếu cần
INSERT INTO VAITRO VALUES('VT001', N'Chủ dịch vụ', N'Toàn quyền');
INSERT INTO VAITRO VALUES('VT002', N'Quản trị viên', N'Quản lý ứng dụng, giám sát.');
INSERT INTO VAITRO VALUES('VT003', N'Người dùng', N'Nghe, gọi, nhắn tin.');

-- TAIKHOAN (mật khẩu mẫu: bạn sẽ hash trên server trong thực tế)
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO) VALUES('TK001', N'giamdoc', '2313131', 'VT001');
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO) VALUES('TK002', N'quantrivien1', '27577451', 'VT002');
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO) VALUES('TK003', N'nguoidung1', '23175437', 'VT003');
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO) VALUES('TK004', N'nguoidung2', '23174745', 'VT003');
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO) VALUES('TK005', N'nguoidung3', '23046906', 'VT003');

-- PHONGBAN
INSERT INTO PHONGBAN VALUES('PB001', N'Phòng quản trị');
INSERT INTO PHONGBAN VALUES('PB002', N'Kế toán');
INSERT INTO PHONGBAN VALUES('PB003', N'Kinh doanh');

-- CHUCVU
INSERT INTO CHUCVU VALUES('CV001', N'Giám đốc');
INSERT INTO CHUCVU VALUES('CV002', N'Quản trị viên');
INSERT INTO CHUCVU VALUES('CV003', N'Trưởng phòng');
INSERT INTO CHUCVU VALUES('CV004', N'Phó phòng');
INSERT INTO CHUCVU VALUES('CV005', N'Nhân viên');

-- PHAN_QUYEN_NHOM (mặc định cần có)
INSERT INTO PHAN_QUYEN_NHOM VALUES('OWNER', 'Chủ nhóm', 1,1,1,1,1,1,1,1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('ADMIN', 'Quản trị viên nhóm', 1,1,1,0,1,1,1,1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('MEMBER', 'Thành viên', 0,0,0,0,0,0,0,0);
INSERT INTO PHAN_QUYEN_NHOM VALUES('QTV_ROLE', 'Quản trị viên (role)', 1,1,1,1,1,1,1,1);

-- LOAICTC examples (per your model: you can create 10 group types + 1 private)
INSERT INTO LOAICTC VALUES('GROUP', N'Group chat', 'N', N'Cuộc trò chuyện nhóm');
INSERT INTO LOAICTC VALUES('PRIVATE', N'Private chat', 'Y', N'Cuộc trò chuyện riêng tư (2 người)');

-- LOAITN / TRANGTHAI
INSERT INTO LOAITN VALUES('TEXT', N'Text');
INSERT INTO TRANGTHAI VALUES('ACTIVE', N'Active');

-- Example NGUOIDUNG profiles (optional)
INSERT INTO NGUOIDUNG VALUES('TK001', 'PB001', 'CV001', N'Nguyễn Giám Đốc', 'giamdoc@example.com', '0900000001', TO_DATE('1970-01-01','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK002', 'PB001', 'CV002', N'Nguyễn Quản Trị', 'qt1@example.com', '0900000002', TO_DATE('1980-02-02','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK003', 'PB002', 'CV005', N'Người Dùng 1', 'user1@example.com', '0900000003', TO_DATE('1995-03-03','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK004', 'PB003', 'CV005', N'Người Dùng 2', 'user2@example.com', '0900000004', TO_DATE('1996-04-04','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK005', NULL, 'CV005', N'Người Dùng 3', 'user3@example.com', '0900000005', TO_DATE('1997-05-05','YYYY-MM-DD'));

-- Example: create a sample conversation + members (so you can test)
-- Note: MACTC format 'CTC_xxx' as earlier; create a sample group and a private conversation
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY)
VALUES('CTC_0001','GROUP',N'Nhóm dự án A', SYSTIMESTAMP, 'TK001', 'N', 'TK001');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0001', 'TK001', SYSTIMESTAMP, 'owner', 'OWNER');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0001', 'TK002', SYSTIMESTAMP, 'admin', 'ADMIN');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0001', 'TK003', SYSTIMESTAMP, 'member', 'MEMBER');

-- Example private conversation between TK003 and TK004
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY)
VALUES('CTC_P_0001', 'PRIVATE', N'Private TK003-TK004', SYSTIMESTAMP, 'TK003', 'Y', 'TK003');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_P_0001', 'TK003', SYSTIMESTAMP, 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_P_0001', 'TK004', SYSTIMESTAMP, 'member', 'MEMBER');

--------------------------------------------------------------------------------
-- End of seeds.sql
--------------------------------------------------------------------------------
