--------------------------------------------------------------------------------
-- SEEDS_COMPLETE.SQL - DỮ LIỆU MẪU HOÀN CHỈNH
-- Chạy SAU schema_complete.sql (kết nối với ChatApplication)
-- 
-- Bao gồm: Vai trò, Tài khoản, Phòng ban, Chức vụ, Cuộc trò chuyện, Tin nhắn
--------------------------------------------------------------------------------

-- ============================================================================
-- 1) VAI TRÒ TRONG HỆ THỐNG
-- ============================================================================
INSERT INTO VAITRO VALUES('VT001', N'Chủ dịch vụ', N'Toàn quyền quản trị hệ thống, quản lý VPD/FGA');
INSERT INTO VAITRO VALUES('VT002', N'Quản trị viên', N'Quản lý người dùng, giám sát hoạt động');
INSERT INTO VAITRO VALUES('VT003', N'Người dùng', N'Sử dụng chat, nhắn tin, gọi điện');

-- ============================================================================
-- 2) PHÒNG BAN
-- ============================================================================
INSERT INTO PHONGBAN(MAPB, TENPB, MOTA) VALUES('PB001', N'Ban Giám Đốc', N'Lãnh đạo công ty');
INSERT INTO PHONGBAN(MAPB, TENPB, MOTA) VALUES('PB002', N'Phòng Kế Toán', N'Quản lý tài chính, kế toán');
INSERT INTO PHONGBAN(MAPB, TENPB, MOTA) VALUES('PB003', N'Phòng Kinh Doanh', N'Bán hàng, marketing');
INSERT INTO PHONGBAN(MAPB, TENPB, MOTA) VALUES('PB004', N'Phòng Nhân Sự', N'Tuyển dụng, quản lý nhân viên');
INSERT INTO PHONGBAN(MAPB, TENPB, MOTA) VALUES('PB005', N'Phòng IT', N'Công nghệ thông tin, hỗ trợ kỹ thuật');

-- ============================================================================
-- 3) CHỨC VỤ
-- ============================================================================
INSERT INTO CHUCVU(MACV, TENCV, CAPBAC, MOTA) VALUES('CV001', N'Giám Đốc', 10, N'Lãnh đạo cao nhất');
INSERT INTO CHUCVU(MACV, TENCV, CAPBAC, MOTA) VALUES('CV002', N'Phó Giám Đốc', 9, N'Phó lãnh đạo');
INSERT INTO CHUCVU(MACV, TENCV, CAPBAC, MOTA) VALUES('CV003', N'Trưởng Phòng', 7, N'Quản lý phòng ban');
INSERT INTO CHUCVU(MACV, TENCV, CAPBAC, MOTA) VALUES('CV004', N'Phó Phòng', 6, N'Phó quản lý phòng ban');
INSERT INTO CHUCVU(MACV, TENCV, CAPBAC, MOTA) VALUES('CV005', N'Nhân Viên', 3, N'Nhân viên chính thức');
INSERT INTO CHUCVU(MACV, TENCV, CAPBAC, MOTA) VALUES('CV006', N'Thực Tập Sinh', 1, N'Nhân viên thực tập');

-- ============================================================================
-- 4) PHÂN QUYỀN NHÓM CHAT
-- ============================================================================
INSERT INTO PHAN_QUYEN_NHOM VALUES('OWNER', N'Chủ nhóm', 1, 1, 1, 1, 1, 1, 1, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('ADMIN', N'Quản trị viên nhóm', 1, 1, 1, 0, 1, 1, 1, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('MODERATOR', N'Điều hành viên', 0, 1, 0, 0, 1, 1, 1, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('MEMBER', N'Thành viên', 0, 0, 0, 0, 0, 0, 0, 0);

-- ============================================================================
-- 5) LOẠI CUỘC TRÒ CHUYỆN
-- ============================================================================
INSERT INTO LOAICTC VALUES('GROUP', N'Nhóm chat', 'N', N'Cuộc trò chuyện nhóm với nhiều thành viên');
INSERT INTO LOAICTC VALUES('PRIVATE', N'Chat riêng tư', 'Y', N'Cuộc trò chuyện riêng tư giữa 2 người');
INSERT INTO LOAICTC VALUES('CHANNEL', N'Kênh', 'N', N'Kênh công khai với nhiều thành viên');
INSERT INTO LOAICTC VALUES('BROADCAST', N'Phát sóng', 'N', N'Kênh phát sóng một chiều');

-- ============================================================================
-- 6) LOẠI TIN NHẮN
-- ============================================================================
INSERT INTO LOAITN VALUES('TEXT', N'Văn bản');
INSERT INTO LOAITN VALUES('IMAGE', N'Hình ảnh');
INSERT INTO LOAITN VALUES('VIDEO', N'Video');
INSERT INTO LOAITN VALUES('AUDIO', N'Âm thanh');
INSERT INTO LOAITN VALUES('FILE', N'Tệp đính kèm');
INSERT INTO LOAITN VALUES('LOCATION', N'Vị trí');
INSERT INTO LOAITN VALUES('CONTACT', N'Danh bạ');
INSERT INTO LOAITN VALUES('ENCRYPTED', N'Tin nhắn mã hóa');

-- ============================================================================
-- 7) TRẠNG THÁI TIN NHẮN
-- ============================================================================
INSERT INTO TRANGTHAI VALUES('ACTIVE', N'Đang hoạt động');
INSERT INTO TRANGTHAI VALUES('DELETED', N'Đã xóa');
INSERT INTO TRANGTHAI VALUES('EDITED', N'Đã chỉnh sửa');
INSERT INTO TRANGTHAI VALUES('HIDDEN', N'Đã ẩn');

-- ============================================================================
-- 8) TÀI KHOẢN NGƯỜI DÙNG
-- Lưu ý: PASSWORD_HASH phải là hash thật (BCrypt)
-- Các giá trị dưới đây là ví dụ, thay bằng hash thực tế khi deploy
-- Password mẫu: "123456" -> hash tương ứng
-- ============================================================================

-- Giám đốc - Clearance Level 5 (cao nhất)
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED) 
VALUES('TK001', 'giamdoc', '$2a$11$example.hash.giamdoc123456', 'VT001', 5, 1);

-- Quản trị viên - Clearance Level 4
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED) 
VALUES('TK002', 'quantrivien', '$2a$11$example.hash.quantrivien123456', 'VT002', 4, 1);

-- Trưởng phòng IT - Clearance Level 4
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED) 
VALUES('TK003', 'truongphongit', '$2a$11$example.hash.truongphongit123456', 'VT003', 4, 1);

-- Nhân viên Kế toán - Clearance Level 3
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED) 
VALUES('TK004', 'nhanvienketoan', '$2a$11$example.hash.nhanvienketoan123456', 'VT003', 3, 1);

-- Nhân viên Kinh doanh - Clearance Level 3
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED) 
VALUES('TK005', 'nhanvienkinhdoanh', '$2a$11$example.hash.nhanvienkinhdoanh123456', 'VT003', 3, 1);

-- Nhân viên IT - Clearance Level 2
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED) 
VALUES('TK006', 'nhanvienit', '$2a$11$example.hash.nhanvienit123456', 'VT003', 2, 1);

-- Thực tập sinh - Clearance Level 1 (thấp nhất)
INSERT INTO TAIKHOAN(MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED) 
VALUES('TK007', 'thuctapsinh', '$2a$11$example.hash.thuctapsinh123456', 'VT003', 1, 1);

-- ============================================================================
-- 9) THÔNG TIN NGƯỜI DÙNG CHI TIẾT
-- ============================================================================
INSERT INTO NGUOIDUNG(MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO) 
VALUES('TK001', 'PB001', 'CV001', N'Nguyễn Văn Minh', 'minh.nguyen@company.com', '0901234567', 
       TO_DATE('1975-05-15','YYYY-MM-DD'), N'123 Nguyễn Huệ, Q1, TP.HCM', N'Giám đốc điều hành công ty');

INSERT INTO NGUOIDUNG(MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO) 
VALUES('TK002', 'PB001', 'CV002', N'Trần Thị Hương', 'huong.tran@company.com', '0902345678', 
       TO_DATE('1980-03-20','YYYY-MM-DD'), N'456 Lê Lợi, Q1, TP.HCM', N'Quản trị viên hệ thống');

INSERT INTO NGUOIDUNG(MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO) 
VALUES('TK003', 'PB005', 'CV003', N'Lê Văn Tuấn', 'tuan.le@company.com', '0903456789', 
       TO_DATE('1985-07-10','YYYY-MM-DD'), N'789 Điện Biên Phủ, Q3, TP.HCM', N'Trưởng phòng IT, chuyên gia bảo mật');

INSERT INTO NGUOIDUNG(MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO) 
VALUES('TK004', 'PB002', 'CV005', N'Phạm Thị Lan', 'lan.pham@company.com', '0904567890', 
       TO_DATE('1990-11-25','YYYY-MM-DD'), N'321 Võ Văn Tần, Q3, TP.HCM', N'Kế toán viên');

INSERT INTO NGUOIDUNG(MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO) 
VALUES('TK005', 'PB003', 'CV005', N'Hoàng Văn Nam', 'nam.hoang@company.com', '0905678901', 
       TO_DATE('1992-09-12','YYYY-MM-DD'), N'654 Cách Mạng Tháng 8, Q10, TP.HCM', N'Nhân viên kinh doanh');

INSERT INTO NGUOIDUNG(MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO) 
VALUES('TK006', 'PB005', 'CV005', N'Võ Thị Mai', 'mai.vo@company.com', '0906789012', 
       TO_DATE('1995-04-30','YYYY-MM-DD'), N'987 Nguyễn Thị Minh Khai, Q1, TP.HCM', N'Lập trình viên');

INSERT INTO NGUOIDUNG(MATK, MAPB, MACV, HOVATEN, EMAIL, SDT, NGAYSINH, DIACHI, BIO) 
VALUES('TK007', 'PB005', 'CV006', N'Đặng Văn Hùng', 'hung.dang@company.com', '0907890123', 
       TO_DATE('2000-12-05','YYYY-MM-DD'), N'147 Trần Hưng Đạo, Q5, TP.HCM', N'Thực tập sinh IT');

-- ============================================================================
-- 10) CÀI ĐẶT NGƯỜI DÙNG MẶC ĐỊNH
-- ============================================================================
INSERT INTO USER_SETTINGS(MATK, THEME, LANGUAGE, ENCRYPTION_ENABLED) VALUES('TK001', 'dark', 'vi', 1);
INSERT INTO USER_SETTINGS(MATK, THEME, LANGUAGE, ENCRYPTION_ENABLED) VALUES('TK002', 'light', 'vi', 1);
INSERT INTO USER_SETTINGS(MATK, THEME, LANGUAGE, ENCRYPTION_ENABLED) VALUES('TK003', 'dark', 'vi', 1);
INSERT INTO USER_SETTINGS(MATK, THEME, LANGUAGE, ENCRYPTION_ENABLED) VALUES('TK004', 'light', 'vi', 0);
INSERT INTO USER_SETTINGS(MATK, THEME, LANGUAGE, ENCRYPTION_ENABLED) VALUES('TK005', 'light', 'vi', 0);
INSERT INTO USER_SETTINGS(MATK, THEME, LANGUAGE, ENCRYPTION_ENABLED) VALUES('TK006', 'dark', 'en', 1);
INSERT INTO USER_SETTINGS(MATK, THEME, LANGUAGE, ENCRYPTION_ENABLED) VALUES('TK007', 'light', 'vi', 0);

-- ============================================================================
-- 11) CUỘC TRÒ CHUYỆN MẪU
-- ============================================================================

-- Nhóm Ban Giám Đốc (nhóm mật - chỉ level 4+ xem được)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY, MOTA, IS_ENCRYPTED)
VALUES('CTC_BGD_001', 'GROUP', N'Ban Giám Đốc', SYSTIMESTAMP, 'TK001', 'N', 'TK001', 
       N'Nhóm trao đổi của Ban Giám Đốc - Thông tin mật', 1);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_BGD_001', 'TK001', 'owner', 'OWNER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_BGD_001', 'TK002', 'admin', 'ADMIN');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_BGD_001', 'TK003', 'member', 'MEMBER');

-- Nhóm Phòng IT
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY, MOTA)
VALUES('CTC_IT_001', 'GROUP', N'Phòng IT', SYSTIMESTAMP, 'TK003', 'N', 'TK003', 
       N'Nhóm trao đổi công việc của Phòng IT');

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_IT_001', 'TK003', 'owner', 'OWNER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_IT_001', 'TK006', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_IT_001', 'TK007', 'member', 'MEMBER');

-- Nhóm Dự Án X (cross-department)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY, MOTA)
VALUES('CTC_PRJ_001', 'GROUP', N'Dự Án X', SYSTIMESTAMP, 'TK002', 'N', 'TK002', 
       N'Nhóm dự án liên phòng ban');

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK002', 'owner', 'OWNER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK003', 'admin', 'ADMIN');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK004', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK005', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_PRJ_001', 'TK006', 'member', 'MEMBER');

-- Chat riêng tư: Giám đốc - Quản trị viên
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY, IS_ENCRYPTED)
VALUES('CTC_P_001', 'PRIVATE', N'Chat: Minh - Hương', SYSTIMESTAMP, 'TK001', 'Y', 'TK001', 1);

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_P_001', 'TK001', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_P_001', 'TK002', 'member', 'MEMBER');

-- Chat riêng tư: Nhân viên IT
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGAYTAO, NGUOIQL, IS_PRIVATE, CREATED_BY)
VALUES('CTC_P_002', 'PRIVATE', N'Chat: Mai - Hùng', SYSTIMESTAMP, 'TK006', 'Y', 'TK006');

INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_P_002', 'TK006', 'member', 'MEMBER');
INSERT INTO THANHVIEN (MACTC, MATK, QUYEN, MAPHANQUYEN) VALUES('CTC_P_002', 'TK007', 'member', 'MEMBER');

-- ============================================================================
-- 12) TIN NHẮN MẪU (với các mức bảo mật khác nhau)
-- Phải set MAC context trước khi INSERT để trigger không chặn
-- ============================================================================

-- === Tin nhắn từ Giám đốc (Level 5) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK001', 5); END;
/

-- Tin nhắn công khai trong nhóm BGD
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK001', 'TEXT', 'ACTIVE', N'Chào mừng mọi người đến với nhóm Ban Giám Đốc!', 3);

-- Tin nhắn MẬT (chỉ level 5 xem được)
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK001', 'TEXT', 'ACTIVE', N'[TỐI MẬT] Ngân sách Q4: 10 tỷ đồng. Không chia sẻ!', 5);

-- Tin nhắn riêng tư với Quản trị viên
INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_001', 'TK001', 'TEXT', 'ACTIVE', N'Hương ơi, chuẩn bị báo cáo cho cuộc họp chiều nay nhé.', 4);

-- === Tin nhắn từ Quản trị viên (Level 4) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK002', 4); END;
/

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK002', 'TEXT', 'ACTIVE', N'Dạ em đã nhận được thông báo. Cuộc họp lúc 14h ạ.', 3);

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_001', 'TK002', 'TEXT', 'ACTIVE', N'Dạ, em sẽ chuẩn bị xong trước 13h ạ.', 4);

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK002', 'TEXT', 'ACTIVE', N'Chào mọi người, đây là nhóm Dự Án X. Mọi người cập nhật tiến độ hàng tuần nhé!', 3);

-- === Tin nhắn từ Trưởng phòng IT (Level 4) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK003', 4); END;
/

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK003', 'TEXT', 'ACTIVE', N'Team IT, tuần này focus vào security audit nhé!', 2);

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK003', 'TEXT', 'ACTIVE', N'[NỘI BỘ] Server credentials đã được update. Check email.', 4);

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK003', 'TEXT', 'ACTIVE', N'Phần backend đã hoàn thành 80%. Dự kiến xong cuối tuần.', 3);

-- === Tin nhắn từ Nhân viên Kế toán (Level 3) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK004', 3); END;
/

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK004', 'TEXT', 'ACTIVE', N'Báo cáo tài chính dự án đã gửi qua email ạ.', 3);

-- === Tin nhắn từ Nhân viên Kinh doanh (Level 3) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK005', 3); END;
/

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK005', 'TEXT', 'ACTIVE', N'Khách hàng đã confirm hợp đồng. Tiến hành triển khai!', 3);

-- === Tin nhắn từ Nhân viên IT (Level 2) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK006', 2); END;
/

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK006', 'TEXT', 'ACTIVE', N'Em đã fix bug #1234. Anh review giúp em với ạ.', 2);

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_002', 'TK006', 'TEXT', 'ACTIVE', N'Hùng ơi, task hôm nay làm đến đâu rồi?', 1);

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_PRJ_001', 'TK006', 'TEXT', 'ACTIVE', N'API endpoints đã deploy lên staging server.', 2);

-- === Tin nhắn từ Thực tập sinh (Level 1) ===
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK007', 1); END;
/

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK007', 'TEXT', 'ACTIVE', N'Em đang học về React, có gì không hiểu em hỏi ạ.', 1);

INSERT INTO TINNHAN(MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_P_002', 'TK007', 'TEXT', 'ACTIVE', N'Em làm được 70% rồi chị ơi. Còn phần UI thôi.', 1);

-- ============================================================================
-- 13) XÓA CONTEXT VÀ COMMIT
-- ============================================================================
BEGIN MAC_CTX_PKG.CLEAR_CONTEXT; END;
/

COMMIT;

--------------------------------------------------------------------------------
-- TỔNG KẾT DỮ LIỆU MẪU:
-- - 3 vai trò (VT001-VT003)
-- - 5 phòng ban (PB001-PB005)
-- - 6 chức vụ (CV001-CV006)
-- - 7 tài khoản với clearance level từ 1-5
-- - 4 loại phân quyền nhóm (OWNER, ADMIN, MODERATOR, MEMBER)
-- - 4 loại cuộc trò chuyện
-- - 8 loại tin nhắn
-- - 4 trạng thái tin nhắn
-- - 5 cuộc trò chuyện (3 nhóm, 2 chat riêng tư)
-- - 17 tin nhắn mẫu với các mức bảo mật khác nhau
-- - 7 cài đặt người dùng
--
-- CLEARANCE LEVELS:
-- Level 5: Giám đốc (TK001) - Xem tất cả
-- Level 4: Quản trị viên, Trưởng phòng (TK002, TK003) - Xem level 1-4
-- Level 3: Nhân viên chính thức (TK004, TK005) - Xem level 1-3
-- Level 2: Nhân viên IT (TK006) - Xem level 1-2
-- Level 1: Thực tập sinh (TK007) - Chỉ xem level 1
--------------------------------------------------------------------------------
