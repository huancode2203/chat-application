--------------------------------------------------------------------------------
-- SEEDS.SQL - DỮ LIỆU MẪU CHO CHAT APPLICATION
-- Bao gồm: Vai trò, Phòng ban, Chức vụ, Tài khoản, Cuộc trò chuyện, Tin nhắn
-- 
-- HƯỚNG DẪN THỰC THI: Chạy SAU schema.sql, procedures.sql, policy.sql
-- Kết nối với user ChatApplication
--------------------------------------------------------------------------------

-- ============================================================================
-- 1) VAI TRÒ TRONG HỆ THỐNG (RBAC)
-- ============================================================================
INSERT INTO VAITRO (MAVAITRO, TENVAITRO, CHUCNANG, CAPDO, MOTA)
VALUES('VT001', N'Chủ dịch vụ', N'Toàn quyền quản trị hệ thống, quản lý VPD/FGA/OLS, cấu hình policy', 10, 
       N'Người có quyền cao nhất trong hệ thống, quản lý toàn bộ chính sách bảo mật');

INSERT INTO VAITRO (MAVAITRO, TENVAITRO, CHUCNANG, CAPDO, MOTA)
VALUES('VT002', N'Quản trị viên', N'Quản lý người dùng, giám sát hoạt động, xem audit logs', 8, 
       N'Quản trị viên hệ thống, hỗ trợ kỹ thuật');

INSERT INTO VAITRO (MAVAITRO, TENVAITRO, CHUCNANG, CAPDO, MOTA)
VALUES('VT003', N'Người dùng', N'Sử dụng chat, nhắn tin, gọi điện, tạo nhóm', 5, 
       N'Người dùng thông thường của ứng dụng');

INSERT INTO VAITRO (MAVAITRO, TENVAITRO, CHUCNANG, CAPDO, MOTA)
VALUES('VT004', N'Khách', N'Chỉ xem, không gửi tin nhắn', 1, 
       N'Tài khoản khách, quyền hạn chế');

-- ============================================================================
-- 2) PHÒNG BAN
-- ============================================================================
INSERT INTO PHONGBAN (MAPB, TENPB, MOTA, CLEARANCELEVEL_MIN)
VALUES('PB001', N'Ban Giám Đốc', N'Lãnh đạo công ty, hoạch định chiến lược', 4);

INSERT INTO PHONGBAN (MAPB, TENPB, MOTA, CLEARANCELEVEL_MIN)
VALUES('PB002', N'Phòng Kế Toán', N'Quản lý tài chính, báo cáo kế toán', 3);

INSERT INTO PHONGBAN (MAPB, TENPB, MOTA, CLEARANCELEVEL_MIN)
VALUES('PB003', N'Phòng Kinh Doanh', N'Bán hàng, marketing, chăm sóc khách hàng', 2);

INSERT INTO PHONGBAN (MAPB, TENPB, MOTA, CLEARANCELEVEL_MIN)
VALUES('PB004', N'Phòng Nhân Sự', N'Tuyển dụng, đào tạo, quản lý nhân viên', 3);

INSERT INTO PHONGBAN (MAPB, TENPB, MOTA, CLEARANCELEVEL_MIN)
VALUES('PB005', N'Phòng Công Nghệ', N'Phát triển phần mềm, hỗ trợ kỹ thuật, bảo mật', 3);

INSERT INTO PHONGBAN (MAPB, TENPB, MOTA, CLEARANCELEVEL_MIN)
VALUES('PB006', N'Phòng Hành Chính', N'Quản lý văn phòng, hậu cần', 1);

-- ============================================================================
-- 3) CHỨC VỤ
-- ============================================================================
INSERT INTO CHUCVU (MACV, TENCV, CAPBAC, CLEARANCELEVEL_DEFAULT, MOTA)
VALUES('CV001', N'Giám Đốc', 10, 5, N'Lãnh đạo cao nhất, quyết định chiến lược');

INSERT INTO CHUCVU (MACV, TENCV, CAPBAC, CLEARANCELEVEL_DEFAULT, MOTA)
VALUES('CV002', N'Phó Giám Đốc', 9, 4, N'Hỗ trợ Giám đốc, quản lý các phòng ban');

INSERT INTO CHUCVU (MACV, TENCV, CAPBAC, CLEARANCELEVEL_DEFAULT, MOTA)
VALUES('CV003', N'Trưởng Phòng', 7, 4, N'Quản lý phòng ban, báo cáo lên ban giám đốc');

INSERT INTO CHUCVU (MACV, TENCV, CAPBAC, CLEARANCELEVEL_DEFAULT, MOTA)
VALUES('CV004', N'Phó Phòng', 6, 3, N'Hỗ trợ trưởng phòng, quản lý nhóm');

INSERT INTO CHUCVU (MACV, TENCV, CAPBAC, CLEARANCELEVEL_DEFAULT, MOTA)
VALUES('CV005', N'Chuyên Viên', 4, 3, N'Nhân viên có chuyên môn cao');

INSERT INTO CHUCVU (MACV, TENCV, CAPBAC, CLEARANCELEVEL_DEFAULT, MOTA)
VALUES('CV006', N'Nhân Viên', 3, 2, N'Nhân viên chính thức');

INSERT INTO CHUCVU (MACV, TENCV, CAPBAC, CLEARANCELEVEL_DEFAULT, MOTA)
VALUES('CV007', N'Thực Tập Sinh', 1, 1, N'Nhân viên thực tập, đang học việc');

-- ============================================================================
-- 4) PHÂN QUYỀN NHÓM CHAT (RBAC cho nhóm)
-- ============================================================================
INSERT INTO PHAN_QUYEN_NHOM (MAPHANQUYEN, TENQUYEN, CAN_ADD, CAN_REMOVE, CAN_PROMOTE, CAN_DELETE, CAN_BAN, CAN_UNBAN, CAN_MUTE, CAN_UNMUTE, CAN_EDIT, CAN_PIN)
VALUES('OWNER', N'Chủ nhóm', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);

INSERT INTO PHAN_QUYEN_NHOM (MAPHANQUYEN, TENQUYEN, CAN_ADD, CAN_REMOVE, CAN_PROMOTE, CAN_DELETE, CAN_BAN, CAN_UNBAN, CAN_MUTE, CAN_UNMUTE, CAN_EDIT, CAN_PIN)
VALUES('ADMIN', N'Quản trị viên nhóm', 1, 1, 1, 0, 1, 1, 1, 1, 1, 1);

INSERT INTO PHAN_QUYEN_NHOM (MAPHANQUYEN, TENQUYEN, CAN_ADD, CAN_REMOVE, CAN_PROMOTE, CAN_DELETE, CAN_BAN, CAN_UNBAN, CAN_MUTE, CAN_UNMUTE, CAN_EDIT, CAN_PIN)
VALUES('MODERATOR', N'Điều hành viên', 0, 1, 0, 0, 1, 1, 1, 1, 0, 1);

INSERT INTO PHAN_QUYEN_NHOM (MAPHANQUYEN, TENQUYEN, CAN_ADD, CAN_REMOVE, CAN_PROMOTE, CAN_DELETE, CAN_BAN, CAN_UNBAN, CAN_MUTE, CAN_UNMUTE, CAN_EDIT, CAN_PIN)
VALUES('MEMBER', N'Thành viên', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

-- ============================================================================
-- 5) LOẠI CUỘC TRÒ CHUYỆN
-- ============================================================================
INSERT INTO LOAICTC (MALOAICTC, TENLOAICTC, IS_PRIVATE, MOTA)
VALUES('GROUP', N'Nhóm chat', 'N', N'Cuộc trò chuyện nhóm với nhiều thành viên');

INSERT INTO LOAICTC (MALOAICTC, TENLOAICTC, IS_PRIVATE, MOTA)
VALUES('PRIVATE', N'Chat riêng tư', 'Y', N'Cuộc trò chuyện riêng tư giữa 2 người');

INSERT INTO LOAICTC (MALOAICTC, TENLOAICTC, IS_PRIVATE, MOTA)
VALUES('CHANNEL', N'Kênh thông báo', 'N', N'Kênh công khai để phát thông báo');

INSERT INTO LOAICTC (MALOAICTC, TENLOAICTC, IS_PRIVATE, MOTA)
VALUES('BROADCAST', N'Phát sóng', 'N', N'Kênh phát sóng một chiều từ admin');

INSERT INTO LOAICTC (MALOAICTC, TENLOAICTC, IS_PRIVATE, MOTA)
VALUES('SUPPORT', N'Hỗ trợ khách hàng', 'Y', N'Kênh hỗ trợ kỹ thuật');

-- ============================================================================
-- 6) LOẠI TIN NHẮN
-- ============================================================================
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('TEXT', N'Văn bản');
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('IMAGE', N'Hình ảnh');
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('VIDEO', N'Video');
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('AUDIO', N'Âm thanh');
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('FILE', N'Tệp đính kèm');
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('LOCATION', N'Vị trí');
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('CONTACT', N'Danh bạ');
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('ENCRYPTED', N'Tin nhắn mã hóa');
INSERT INTO LOAITN (MALOAITN, TENLOAITN) VALUES('SYSTEM', N'Thông báo hệ thống');

-- ============================================================================
-- 7) TRẠNG THÁI TIN NHẮN
-- ============================================================================
INSERT INTO TRANGTHAI (MATRANGTHAI, TENTRANGTHAI) VALUES('ACTIVE', N'Đang hoạt động');
INSERT INTO TRANGTHAI (MATRANGTHAI, TENTRANGTHAI) VALUES('DELETED', N'Đã xóa');
INSERT INTO TRANGTHAI (MATRANGTHAI, TENTRANGTHAI) VALUES('EDITED', N'Đã chỉnh sửa');
INSERT INTO TRANGTHAI (MATRANGTHAI, TENTRANGTHAI) VALUES('HIDDEN', N'Đã ẩn');
INSERT INTO TRANGTHAI (MATRANGTHAI, TENTRANGTHAI) VALUES('PENDING', N'Đang chờ duyệt');
INSERT INTO TRANGTHAI (MATRANGTHAI, TENTRANGTHAI) VALUES('RECALLED', N'Đã thu hồi');

-- ============================================================================
-- 8) TÀI KHOẢN NGƯỜI DÙNG
-- Lưu ý: PASSWORD_HASH là BCrypt hash của "123456"
-- Thay bằng hash thực tế khi triển khai
-- ============================================================================

-- Chủ dịch vụ - Clearance Level 5 (cao nhất)
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED, PROFILE_NAME)
VALUES('TK001', 'giamdoc', '$2a$11$example.hash.giamdoc123456', 'VT001', 5, 1, 'CHAT_ADMIN_PROFILE');

-- Quản trị viên - Clearance Level 4
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED, PROFILE_NAME)
VALUES('TK002', 'quantrivien', '$2a$11$example.hash.quantrivien123456', 'VT002', 4, 1, 'CHAT_ADMIN_PROFILE');

-- Phó Giám đốc - Clearance Level 4
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK003', 'phogiamdoc', '$2a$11$example.hash.phogiamdoc123456', 'VT003', 4, 1);

-- Trưởng phòng IT - Clearance Level 4
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK004', 'truongphongit', '$2a$11$example.hash.truongphongit123456', 'VT003', 4, 1);

-- Trưởng phòng Kế toán - Clearance Level 4
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK005', 'truongphongkt', '$2a$11$example.hash.truongphongkt123456', 'VT003', 4, 1);

-- Chuyên viên Kế toán - Clearance Level 3
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK006', 'chuyenvienkt', '$2a$11$example.hash.chuyenvienkt123456', 'VT003', 3, 1);

-- Nhân viên Kinh doanh - Clearance Level 2
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK007', 'nhanvienkd', '$2a$11$example.hash.nhanvienkd123456', 'VT003', 2, 1);

-- Nhân viên IT - Clearance Level 3
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK008', 'nhanvienit', '$2a$11$example.hash.nhanvienit123456', 'VT003', 3, 1);

-- Nhân viên Hành chính - Clearance Level 2
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK009', 'nhanvienhc', '$2a$11$example.hash.nhanvienhc123456', 'VT003', 2, 1);

-- Thực tập sinh IT - Clearance Level 1 (thấp nhất)
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED, PROFILE_NAME)
VALUES('TK010', 'thuctapsinhit', '$2a$11$example.hash.thuctapsinhit123456', 'VT003', 1, 1, 'CHAT_INTERN_PROFILE');

-- Thực tập sinh Kinh doanh - Clearance Level 1
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED, PROFILE_NAME)
VALUES('TK011', 'thuctapsinhkd', '$2a$11$example.hash.thuctapsinhkd123456', 'VT003', 1, 1, 'CHAT_INTERN_PROFILE');

-- ============================================================================
-- 9) THÔNG TIN NGƯỜI DÙNG CHI TIẾT
-- ============================================================================
INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK001', 'PB001', 'CV001', N'Nguyễn Văn Minh', 'minh.nguyen@company.com', '0901234567', 
       TO_DATE('1975-05-15','YYYY-MM-DD'), N'123 Nguyễn Huệ, Quận 1, TP.HCM', 
       N'Giám đốc điều hành với 20 năm kinh nghiệm');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK002', 'PB005', 'CV003', N'Trần Thị Hương', 'huong.tran@company.com', '0902345678', 
       TO_DATE('1980-03-20','YYYY-MM-DD'), N'456 Lê Lợi, Quận 1, TP.HCM', 
       N'Quản trị viên hệ thống, chuyên gia bảo mật');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK003', 'PB001', 'CV002', N'Lê Văn Tuấn', 'tuan.le@company.com', '0903456789', 
       TO_DATE('1978-07-10','YYYY-MM-DD'), N'789 Điện Biên Phủ, Quận 3, TP.HCM', 
       N'Phó Giám đốc phụ trách kinh doanh');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK004', 'PB005', 'CV003', N'Phạm Văn Đức', 'duc.pham@company.com', '0904567890', 
       TO_DATE('1985-11-25','YYYY-MM-DD'), N'321 Võ Văn Tần, Quận 3, TP.HCM', 
       N'Trưởng phòng IT, kiến trúc sư phần mềm');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK005', 'PB002', 'CV003', N'Hoàng Thị Lan', 'lan.hoang@company.com', '0905678901', 
       TO_DATE('1982-09-12','YYYY-MM-DD'), N'654 Cách Mạng Tháng 8, Quận 10, TP.HCM', 
       N'Trưởng phòng Kế toán, CPA');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK006', 'PB002', 'CV005', N'Võ Văn Nam', 'nam.vo@company.com', '0906789012', 
       TO_DATE('1990-04-30','YYYY-MM-DD'), N'987 Nguyễn Thị Minh Khai, Quận 1, TP.HCM', 
       N'Chuyên viên kế toán tổng hợp');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK007', 'PB003', 'CV006', N'Đặng Thị Mai', 'mai.dang@company.com', '0907890123', 
       TO_DATE('1992-12-05','YYYY-MM-DD'), N'147 Trần Hưng Đạo, Quận 5, TP.HCM', 
       N'Nhân viên kinh doanh, phụ trách khu vực miền Nam');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK008', 'PB005', 'CV006', N'Nguyễn Văn Hùng', 'hung.nguyen@company.com', '0908901234', 
       TO_DATE('1993-08-18','YYYY-MM-DD'), N'258 Hai Bà Trưng, Quận 1, TP.HCM', 
       N'Lập trình viên Full-stack');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK009', 'PB006', 'CV006', N'Trần Văn Bình', 'binh.tran@company.com', '0909012345', 
       TO_DATE('1991-06-22','YYYY-MM-DD'), N'369 Lý Tự Trọng, Quận 1, TP.HCM', 
       N'Nhân viên hành chính văn phòng');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK010', 'PB005', 'CV007', N'Lê Thị Hoa', 'hoa.le@company.com', '0910123456', 
       TO_DATE('2000-02-14','YYYY-MM-DD'), N'741 Nguyễn Văn Cừ, Quận 5, TP.HCM', 
       N'Thực tập sinh phát triển phần mềm');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO)
VALUES('TK011', 'PB003', 'CV007', N'Phạm Văn Khoa', 'khoa.pham@company.com', '0911234567', 
       TO_DATE('2001-10-08','YYYY-MM-DD'), N'852 Nguyễn Trãi, Quận 5, TP.HCM', 
       N'Thực tập sinh kinh doanh');

-- ============================================================================
-- 10) CUỘC TRÒ CHUYỆN MẪU
-- ============================================================================

-- Nhóm Ban Giám Đốc (Mật - MIN_CLEARANCE = 4)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MOTA, IS_ENCRYPTED, MIN_CLEARANCE)
VALUES('CTC_BGD_001', 'GROUP', N'Ban Giám Đốc', 'TK001', 'N', 'TK001', 
       N'Nhóm trao đổi của Ban Giám Đốc - Thông tin mật', 1, 4);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_BGD_001', 'TK001', 'owner', 'OWNER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_BGD_001', 'TK002', 'admin', 'ADMIN');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_BGD_001', 'TK003', 'member', 'MEMBER');

-- Nhóm Phòng IT (MIN_CLEARANCE = 2)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MOTA, MIN_CLEARANCE)
VALUES('CTC_IT_001', 'GROUP', N'Phòng Công Nghệ', 'TK004', 'N', 'TK004', 
       N'Nhóm trao đổi công việc của Phòng IT', 2);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_IT_001', 'TK004', 'owner', 'OWNER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_IT_001', 'TK008', 'admin', 'ADMIN');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_IT_001', 'TK010', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_IT_001', 'TK002', 'member', 'MEMBER');

-- Nhóm Phòng Kế Toán (MIN_CLEARANCE = 3)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MOTA, MIN_CLEARANCE)
VALUES('CTC_KT_001', 'GROUP', N'Phòng Kế Toán', 'TK005', 'N', 'TK005', 
       N'Nhóm trao đổi nghiệp vụ kế toán', 3);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_KT_001', 'TK005', 'owner', 'OWNER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_KT_001', 'TK006', 'member', 'MEMBER');

-- Nhóm Dự Án Alpha (liên phòng ban)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MOTA, MIN_CLEARANCE)
VALUES('CTC_PRJ_001', 'GROUP', N'Dự Án Alpha', 'TK003', 'N', 'TK003', 
       N'Nhóm dự án liên phòng ban - Phát triển sản phẩm mới', 2);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK003', 'owner', 'OWNER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK004', 'admin', 'ADMIN');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK005', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK007', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK008', 'member', 'MEMBER');

-- Kênh thông báo công ty
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MOTA, MIN_CLEARANCE)
VALUES('CTC_CHANNEL_001', 'CHANNEL', N'Thông Báo Công Ty', 'TK001', 'N', 'TK001', 
       N'Kênh thông báo chung cho toàn công ty', 1);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_CHANNEL_001', 'TK001', 'owner', 'OWNER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_CHANNEL_001', 'TK002', 'admin', 'ADMIN');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_CHANNEL_001', 'TK003', 'admin', 'ADMIN');

-- Chat riêng tư: Giám đốc - Phó Giám đốc
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, IS_ENCRYPTED, MIN_CLEARANCE)
VALUES('CTC_P_001', 'PRIVATE', N'Chat: Minh - Tuấn', 'TK001', 'Y', 'TK001', 1, 4);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_P_001', 'TK001', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_P_001', 'TK003', 'member', 'MEMBER');

-- Chat riêng tư: Nhân viên IT
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MIN_CLEARANCE)
VALUES('CTC_P_002', 'PRIVATE', N'Chat: Hùng - Hoa', 'TK008', 'Y', 'TK008', 1);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_P_002', 'TK008', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_P_002', 'TK010', 'member', 'MEMBER');

-- ============================================================================
-- 11) TIN NHẮN MẪU (với các mức bảo mật khác nhau)
-- Phải set MAC context trước khi INSERT để trigger không chặn
-- ============================================================================

-- === Tin nhắn từ Giám đốc (Level 5) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK001', 5); END;
/

-- Tin nhắn công khai (Level 1)
INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_CHANNEL_001', 'TK001', 'TEXT', 'ACTIVE', 
       N'Chào mừng tất cả mọi người đến với hệ thống chat nội bộ công ty!', 1);

-- Tin nhắn trong Ban Giám Đốc (Level 4)
INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK001', 'TEXT', 'ACTIVE', 
       N'[NỘI BỘ] Cuộc họp chiến lược Q4 sẽ diễn ra vào thứ 6 tuần này.', 4);

-- Tin nhắn TỐI MẬT (Level 5)
INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK001', 'TEXT', 'ACTIVE', 
       N'[TỐI MẬT] Ngân sách dự kiến cho dự án M&A: 50 tỷ đồng. Tuyệt đối bảo mật!', 5);

-- Tin nhắn riêng tư với Phó Giám đốc
INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_001', 'TK001', 'TEXT', 'ACTIVE', 
       N'Tuấn ơi, anh cần em chuẩn bị báo cáo doanh thu Q3 trước thứ 4 nhé.', 4);

-- === Tin nhắn từ Quản trị viên (Level 4) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK002', 4); END;
/

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK002', 'TEXT', 'ACTIVE', 
       N'Dạ em đã hoàn thành audit security cho hệ thống. Báo cáo đã gửi qua email ạ.', 4);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK002', 'TEXT', 'ACTIVE', 
       N'[THÔNG BÁO] Hệ thống sẽ bảo trì từ 22h-24h tối nay. Mọi người lưu ý!', 2);

-- === Tin nhắn từ Phó Giám đốc (Level 4) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK003', 4); END;
/

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK003', 'TEXT', 'ACTIVE', 
       N'Báo cáo doanh thu Q3 đã hoàn thành. Tăng trưởng 15% so với cùng kỳ.', 4);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_001', 'TK003', 'TEXT', 'ACTIVE', 
       N'Dạ em đã chuẩn bị xong báo cáo. Em gửi file cho anh xem trước nhé.', 4);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK003', 'TEXT', 'ACTIVE', 
       N'Chào team! Dự án Alpha chính thức khởi động. Mọi người cập nhật tiến độ hàng tuần nhé!', 2);

-- === Tin nhắn từ Trưởng phòng IT (Level 4) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK004', 4); END;
/

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK004', 'TEXT', 'ACTIVE', 
       N'Team IT, tuần này focus vào security audit cho hệ thống mới nhé!', 2);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK004', 'TEXT', 'ACTIVE', 
       N'[NỘI BỘ] Server credentials đã được update. Mọi người check email để lấy thông tin mới.', 4);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK004', 'TEXT', 'ACTIVE', 
       N'Phần backend đã hoàn thành 80%. Dự kiến hoàn thiện vào cuối tuần.', 2);

-- === Tin nhắn từ Trưởng phòng Kế toán (Level 4) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK005', 4); END;
/

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_KT_001', 'TK005', 'TEXT', 'ACTIVE', 
       N'Team kế toán, deadline báo cáo thuế là ngày 20 tháng này. Mọi người hoàn thành sớm nhé!', 3);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK005', 'TEXT', 'ACTIVE', 
       N'Báo cáo chi phí dự án Alpha đã được cập nhật. Tổng chi phí hiện tại: 500 triệu.', 3);

-- === Tin nhắn từ Chuyên viên Kế toán (Level 3) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK006', 3); END;
/

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_KT_001', 'TK006', 'TEXT', 'ACTIVE', 
       N'Dạ em đã hoàn thành đối soát công nợ tháng này ạ.', 3);

-- === Tin nhắn từ Nhân viên Kinh doanh (Level 2) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK007', 2); END;
/

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK007', 'TEXT', 'ACTIVE', 
       N'Khách hàng ABC đã confirm đơn hàng 200 triệu. Tiến hành ký hợp đồng tuần sau!', 2);

-- === Tin nhắn từ Nhân viên IT (Level 3) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK008', 3); END;
/

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK008', 'TEXT', 'ACTIVE', 
       N'Em đã fix xong bug #1234. Anh Đức review giúp em với ạ.', 2);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_002', 'TK008', 'TEXT', 'ACTIVE', 
       N'Hoa ơi, task hôm nay em làm đến đâu rồi?', 1);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK008', 'TEXT', 'ACTIVE', 
       N'API endpoints đã deploy lên staging server. Mọi người test thử nhé!', 2);

-- === Tin nhắn từ Thực tập sinh (Level 1) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK010', 1); END;
/

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK010', 'TEXT', 'ACTIVE', 
       N'Em đang học về React và Node.js, có gì không hiểu em sẽ hỏi các anh chị ạ.', 1);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_002', 'TK010', 'TEXT', 'ACTIVE', 
       N'Em làm được 70% task rồi anh ơi. Còn phần UI design thôi ạ.', 1);

-- ============================================================================
-- 12) XÓA CONTEXT VÀ COMMIT
-- ============================================================================
BEGIN MAC_CTX_PKG.CLEAR_CONTEXT; END;
/

COMMIT;

--------------------------------------------------------------------------------
-- TỔNG KẾT DỮ LIỆU MẪU:
-- ============================================================================
-- - 4 vai trò (VT001-VT004) với các mức quyền khác nhau
-- - 6 phòng ban (PB001-PB006) với CLEARANCELEVEL_MIN
-- - 7 chức vụ (CV001-CV007) với CLEARANCELEVEL_DEFAULT
-- - 4 loại phân quyền nhóm (OWNER, ADMIN, MODERATOR, MEMBER)
-- - 5 loại cuộc trò chuyện
-- - 9 loại tin nhắn
-- - 6 trạng thái tin nhắn
-- - 11 tài khoản với clearance level từ 1-5
-- - 7 cuộc trò chuyện (4 nhóm, 1 kênh, 2 chat riêng tư)
-- - 20+ tin nhắn mẫu với các mức bảo mật khác nhau
--
-- CLEARANCE LEVELS (MAC):
-- Level 5: Giám đốc (TK001) - Tối Mật - Xem tất cả
-- Level 4: Phó GĐ, Trưởng phòng, Quản trị viên (TK002-TK005) - Mật - Xem level 1-4
-- Level 3: Chuyên viên, Nhân viên IT (TK006, TK008) - Nội bộ - Xem level 1-3
-- Level 2: Nhân viên thường (TK007, TK009) - Công khai nội bộ - Xem level 1-2
-- Level 1: Thực tập sinh (TK010, TK011) - Công khai - Chỉ xem level 1
--------------------------------------------------------------------------------

-- Verify
SELECT 'Seeds inserted successfully!' AS TRANG_THAI FROM DUAL;
SELECT 'Tài khoản: ' || COUNT(*) AS THONG_KE FROM TAIKHOAN;
SELECT 'Cuộc trò chuyện: ' || COUNT(*) AS THONG_KE FROM CUOCTROCHUYEN;
SELECT 'Tin nhắn: ' || COUNT(*) AS THONG_KE FROM TINNHAN;

--------------------------------------------------------------------------------
-- KẾT THÚC SEEDS.SQL
--------------------------------------------------------------------------------
