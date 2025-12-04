--------------------------------------------------------------------------------
-- seeds.sql - DỮ LIỆU MẪU
-- Chạy SAU schema.sql (kết nối với ChatApplication).
-- File này chứa tất cả các câu lệnh INSERT (dữ liệu mẫu)
--------------------------------------------------------------------------------

-- 1) Nhập liệu VAITRO (vai trò trong hệ thống)
-- Lưu ý: Sử dụng N'...' để hỗ trợ Unicode cho tiếng Việt
INSERT INTO VAITRO VALUES('VT001', N'Chủ dịch vụ', N'Toàn quyền quản trị hệ thống');
INSERT INTO VAITRO VALUES('VT002', N'Quản trị viên', N'Quản lý ứng dụng, giám sát người dùng');
INSERT INTO VAITRO VALUES('VT003', N'Người dùng', N'Sử dụng chat, nhắn tin, gọi điện');

-- 2) TAIKHOAN (tài khoản người dùng)
-- Lưu ý: Trong thực tế, PASSWORD_HASH phải là mã hash thật (bcrypt, SHA256, v.v.)
-- Các giá trị dưới đây chỉ là ví dụ, cần thay bằng hash thực tế
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL) 
VALUES('TK001', N'giamdoc', 'hash_password_giamdoc', 'VT001', 5);

INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL) 
VALUES('TK002', N'quantrivien1', 'hash_password_qtv1', 'VT002', 4);

INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL) 
VALUES('TK003', N'nguoidung1', 'hash_password_user1', 'VT003', 3);

INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL) 
VALUES('TK004', N'nguoidung2', 'hash_password_user2', 'VT003', 3);

INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL) 
VALUES('TK005', N'nguoidung3', 'hash_password_user3', 'VT003', 2);

INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL) 
VALUES('TK006', N'nguoidung4', 'hash_password_user4', 'VT003', 1);

-- 3) PHONGBAN (phòng ban trong tổ chức)
INSERT INTO PHONGBAN VALUES('PB001', N'Phòng Quản Trị');
INSERT INTO PHONGBAN VALUES('PB002', N'Phòng Kế Toán');
INSERT INTO PHONGBAN VALUES('PB003', N'Phòng Kinh Doanh');
INSERT INTO PHONGBAN VALUES('PB004', N'Phòng Nhân Sự');
INSERT INTO PHONGBAN VALUES('PB005', N'Phòng IT');

-- 4) CHUCVU (chức vụ trong tổ chức)
INSERT INTO CHUCVU VALUES('CV001', N'Giám Đốc');
INSERT INTO CHUCVU VALUES('CV002', N'Quản Trị Viên');
INSERT INTO CHUCVU VALUES('CV003', N'Trưởng Phòng');
INSERT INTO CHUCVU VALUES('CV004', N'Phó Phòng');
INSERT INTO CHUCVU VALUES('CV005', N'Nhân Viên');
INSERT INTO CHUCVU VALUES('CV006', N'Thực Tập Sinh');

-- 5) PHAN_QUYEN_NHOM (phân quyền trong nhóm chat)
-- Các quyền: CAN_ADD, CAN_REMOVE, CAN_PROMOTE, CAN_DELETE, CAN_BAN, CAN_UNBAN, CAN_MUTE, CAN_UNMUTE
INSERT INTO PHAN_QUYEN_NHOM VALUES('OWNER', N'Chủ nhóm', 1, 1, 1, 1, 1, 1, 1, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('ADMIN', N'Quản trị viên nhóm', 1, 1, 1, 0, 1, 1, 1, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('MODERATOR', N'Điều hành viên', 0, 1, 0, 0, 1, 1, 1, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('MEMBER', N'Thành viên', 0, 0, 0, 0, 0, 0, 0, 0);

-- 6) LOAICTC (loại cuộc trò chuyện)
INSERT INTO LOAICTC VALUES('GROUP', N'Nhóm chat', 'N', N'Cuộc trò chuyện nhóm với nhiều thành viên');
INSERT INTO LOAICTC VALUES('PRIVATE', N'Chat riêng tư', 'Y', N'Cuộc trò chuyện riêng tư giữa 2 người');
INSERT INTO LOAICTC VALUES('CHANNEL', N'Kênh', 'N', N'Kênh công khai với nhiều thành viên');
INSERT INTO LOAICTC VALUES('BROADCAST', N'Phát sóng', 'N', N'Kênh phát sóng một chiều');

-- 7) LOAITN (loại tin nhắn)
INSERT INTO LOAITN VALUES('TEXT', N'Văn bản');
INSERT INTO LOAITN VALUES('IMAGE', N'Hình ảnh');
INSERT INTO LOAITN VALUES('VIDEO', N'Video');
INSERT INTO LOAITN VALUES('AUDIO', N'Âm thanh');
INSERT INTO LOAITN VALUES('FILE', N'Tệp đính kèm');
INSERT INTO LOAITN VALUES('LOCATION', N'Vị trí');
INSERT INTO LOAITN VALUES('CONTACT', N'Danh bạ');

-- 8) TRANGTHAI (trạng thái tin nhắn)
INSERT INTO TRANGTHAI VALUES('ACTIVE', N'Đang hoạt động');
INSERT INTO TRANGTHAI VALUES('DELETED', N'Đã xóa');
INSERT INTO TRANGTHAI VALUES('EDITED', N'Đã chỉnh sửa');
INSERT INTO TRANGTHAI VALUES('HIDDEN', N'Đã ẩn');

-- 9) NGUOIDUNG (thông tin chi tiết người dùng)
INSERT INTO NGUOIDUNG VALUES('TK001', 'PB001', 'CV001', N'Nguyễn Văn Giám Đốc', 'giamdoc@company.com', '0901234567', TO_DATE('1970-01-15','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK002', 'PB001', 'CV002', N'Trần Thị Quản Trị', 'quantri@company.com', '0902345678', TO_DATE('1980-03-20','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK003', 'PB002', 'CV005', N'Lê Văn An', 'an.le@company.com', '0903456789', TO_DATE('1990-05-10','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK004', 'PB003', 'CV005', N'Phạm Thị Bình', 'binh.pham@company.com', '0904567890', TO_DATE('1992-07-25','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK005', 'PB005', 'CV005', N'Hoàng Văn Cường', 'cuong.hoang@company.com', '0905678901', TO_DATE('1995-09-12','YYYY-MM-DD'));
INSERT INTO NGUOIDUNG VALUES('TK006', 'PB003', 'CV006', N'Võ Thị Dung', 'dung.vo@company.com', '0906789012', TO_DATE('1998-11-30','YYYY-MM-DD'));

-- 10) CUOCTROCHUYEN mẫu

-- Nhóm dự án A (nhóm công khai)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY)
VALUES('CTC_0001', 'GROUP', N'Nhóm Dự Án A', SYSTIMESTAMP, 'TK001', 'N', 'TK001');

-- Thêm thành viên vào nhóm dự án A
INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0001', 'TK001', SYSTIMESTAMP, 'owner', 'OWNER');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0001', 'TK002', SYSTIMESTAMP, 'admin', 'ADMIN');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0001', 'TK003', SYSTIMESTAMP, 'member', 'MEMBER');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0001', 'TK004', SYSTIMESTAMP, 'member', 'MEMBER');

-- Nhóm Kế Toán
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY)
VALUES('CTC_0002', 'GROUP', N'Nhóm Kế Toán', SYSTIMESTAMP, 'TK002', 'N', 'TK002');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0002', 'TK002', SYSTIMESTAMP, 'owner', 'OWNER');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_0002', 'TK003', SYSTIMESTAMP, 'member', 'MEMBER');

-- Chat riêng tư giữa TK003 và TK004
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY)
VALUES('CTC_P_0001', 'PRIVATE', N'Chat Riêng: An - Bình', SYSTIMESTAMP, 'TK003', 'Y', 'TK003');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_P_0001', 'TK003', SYSTIMESTAMP, 'member', 'MEMBER');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_P_0001', 'TK004', SYSTIMESTAMP, 'member', 'MEMBER');

-- Chat riêng tư giữa TK005 và TK006
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY)
VALUES('CTC_P_0002', 'PRIVATE', N'Chat Riêng: Cường - Dung', SYSTIMESTAMP, 'TK005', 'Y', 'TK005');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_P_0002', 'TK005', SYSTIMESTAMP, 'member', 'MEMBER');

INSERT INTO THANHVIEN (MACTC, MATK, NGAYTHAMGIA, QUYEN, MAPHANQUYEN)
VALUES('CTC_P_0002', 'TK006', SYSTIMESTAMP, 'member', 'MEMBER');

-- 11) TIN NHẮN mẫu (với các mức bảo mật khác nhau)
-- LƯU Ý: Phải thiết lập MAC context trước khi INSERT tin nhắn
-- để trigger TRG_TINNHAN_CHECK_WRITE_UP không chặn

-- Thiết lập context cho TK001 (clearance level 5)
BEGIN
  MAC_CTX_PKG.SET_USER_LEVEL('TK001', 5);
END;
/

-- Tin nhắn trong nhóm dự án A (từ TK001 - level 5)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_0001', 'TK001', 'TEXT', 'ACTIVE', N'Chào mừng mọi người đến với nhóm dự án A!', 3);

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_0001', 'TK003', 'TEXT', 'ACTIVE', N'Xin chào mọi người, rất vui được tham gia dự án.', 3);

-- Tin nhắn mật (chỉ người có clearance level 5 mới xem được)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_0001', 'TK001', 'TEXT', 'ACTIVE', N'[MẬT] Thông tin ngân sách dự án: 5 tỷ đồng', 5);

-- Thiết lập context cho TK002 (clearance level 4)
BEGIN
  MAC_CTX_PKG.SET_USER_LEVEL('TK002', 4);
END;
/

-- Tin nhắn trong nhóm dự án A (từ TK002 - level 4)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_0001', 'TK002', 'TEXT', 'ACTIVE', N'Cuộc họp đầu tiên sẽ diễn ra vào thứ 2 tuần sau.', 4);

-- Tin nhắn trong nhóm kế toán (từ TK002 - level 4)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_0002', 'TK002', 'TEXT', 'ACTIVE', N'Báo cáo tài chính tháng này đã hoàn thành.', 3);

-- Thiết lập context cho TK003 (clearance level 3)
BEGIN
  MAC_CTX_PKG.SET_USER_LEVEL('TK003', 3);
END;
/

-- Tin nhắn trong nhóm dự án A (từ TK003 - level 3)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_0001', 'TK003', 'TEXT', 'ACTIVE', N'Xin chào mọi người, rất vui được tham gia dự án.', 3);

-- Tin nhắn trong nhóm kế toán (từ TK003 - level 3)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_0002', 'TK003', 'TEXT', 'ACTIVE', N'Em đã kiểm tra và xác nhận số liệu chính xác.', 3);

-- Tin nhắn riêng tư giữa An và Bình (từ TK003 - level 3)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_0001', 'TK003', 'TEXT', 'ACTIVE', N'Bình ơi, chiều nay đi cafe không?', 2);

-- Thiết lập context cho TK004 (clearance level 3)
BEGIN
  MAC_CTX_PKG.SET_USER_LEVEL('TK004', 3);
END;
/

-- Tin nhắn riêng tư (từ TK004 - level 3)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_0001', 'TK004', 'TEXT', 'ACTIVE', N'Được chứ, 5h chiều nhé!', 2);

-- Thiết lập context cho TK005 (clearance level 2)
BEGIN
  MAC_CTX_PKG.SET_USER_LEVEL('TK005', 2);
END;
/

-- Tin nhắn riêng tư giữa Cường và Dung (từ TK005 - level 2)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_0002', 'TK005', 'TEXT', 'ACTIVE', N'Dung có tài liệu về React không?', 1);

-- Thiết lập context cho TK006 (clearance level 1)
BEGIN
  MAC_CTX_PKG.SET_USER_LEVEL('TK006', 1);
END;
/

-- Tin nhắn riêng tư (từ TK006 - level 1)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_0002', 'TK006', 'TEXT', 'ACTIVE', N'Có anh, em gửi link này: https://react.dev', 1);

-- Xóa context sau khi hoàn thành
BEGIN
  MAC_CTX_PKG.CLEAR_CONTEXT;
END;
/

-- 12) COMMIT tất cả thay đổi
COMMIT;

--------------------------------------------------------------------------------
-- Kết thúc seeds.sql
-- 
-- TỔNG KẾT DỮ LIỆU MẪU:
-- - 3 vai trò (VT001-VT003)
-- - 6 tài khoản với clearance level từ 1-5 (TK001-TK006)
-- - 5 phòng ban (PB001-PB005)
-- - 6 chức vụ (CV001-CV006)
-- - 4 loại phân quyền nhóm (OWNER, ADMIN, MODERATOR, MEMBER)
-- - 4 loại cuộc trò chuyện (GROUP, PRIVATE, CHANNEL, BROADCAST)
-- - 7 loại tin nhắn (TEXT, IMAGE, VIDEO, AUDIO, FILE, LOCATION, CONTACT)
-- - 4 trạng thái tin nhắn (ACTIVE, DELETED, EDITED, HIDDEN)
-- - 6 người dùng với thông tin đầy đủ
-- - 4 cuộc trò chuyện (2 nhóm, 2 chat riêng tư)
-- - 10 tin nhắn mẫu với các mức bảo mật khác nhau
--------------------------------------------------------------------------------