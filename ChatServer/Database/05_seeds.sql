--------------------------------------------------------------------------------
-- 05_SEEDS.SQL - CHẠY VỚI ChatApplication
-- Dữ liệu mẫu: Vai trò, Phòng ban, Chức vụ, Tài khoản, Cuộc trò chuyện
--------------------------------------------------------------------------------

-- =============================================================================
-- 1. VAI TRÒ (RBAC)
-- =============================================================================
INSERT INTO VAITRO VALUES('VT001', N'Chủ dịch vụ', N'Toàn quyền quản trị, quản lý VPD/FGA/OLS', 10, N'Người có quyền cao nhất');
INSERT INTO VAITRO VALUES('VT002', N'Quản trị viên', N'Quản lý người dùng, giám sát, xem audit logs', 8, N'Quản trị viên hệ thống');
INSERT INTO VAITRO VALUES('VT003', N'Người dùng', N'Sử dụng chat, nhắn tin, tạo nhóm', 5, N'Người dùng thông thường');
INSERT INTO VAITRO VALUES('VT004', N'Khách', N'Chỉ xem, không gửi tin nhắn', 1, N'Tài khoản khách');

-- =============================================================================
-- 2. PHÒNG BAN
-- =============================================================================
INSERT INTO PHONGBAN VALUES('PB001', N'Ban Giám Đốc', N'Lãnh đạo công ty', 4);
INSERT INTO PHONGBAN VALUES('PB002', N'Phòng Kế Toán', N'Quản lý tài chính', 3);
INSERT INTO PHONGBAN VALUES('PB003', N'Phòng Kinh Doanh', N'Bán hàng, marketing', 2);
INSERT INTO PHONGBAN VALUES('PB004', N'Phòng Nhân Sự', N'Tuyển dụng, đào tạo', 3);
INSERT INTO PHONGBAN VALUES('PB005', N'Phòng Công Nghệ', N'Phát triển phần mềm', 3);
INSERT INTO PHONGBAN VALUES('PB006', N'Phòng Hành Chính', N'Quản lý văn phòng', 1);

-- =============================================================================
-- 3. CHỨC VỤ
-- =============================================================================
INSERT INTO CHUCVU VALUES('CV001', N'Giám Đốc', 10, 5, N'Lãnh đạo cao nhất');
INSERT INTO CHUCVU VALUES('CV002', N'Phó Giám Đốc', 9, 4, N'Hỗ trợ Giám đốc');
INSERT INTO CHUCVU VALUES('CV003', N'Trưởng Phòng', 7, 4, N'Quản lý phòng ban');
INSERT INTO CHUCVU VALUES('CV004', N'Phó Phòng', 6, 3, N'Hỗ trợ trưởng phòng');
INSERT INTO CHUCVU VALUES('CV005', N'Chuyên Viên', 4, 3, N'Nhân viên chuyên môn cao');
INSERT INTO CHUCVU VALUES('CV006', N'Nhân Viên', 3, 2, N'Nhân viên chính thức');
INSERT INTO CHUCVU VALUES('CV007', N'Thực Tập Sinh', 1, 1, N'Nhân viên thực tập');

-- =============================================================================
-- 4. PHÂN QUYỀN NHÓM CHAT
-- =============================================================================
INSERT INTO PHAN_QUYEN_NHOM VALUES('OWNER', N'Chủ nhóm', 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('ADMIN', N'Quản trị viên nhóm', 1, 1, 1, 0, 1, 1, 1, 1, 1, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('MODERATOR', N'Điều hành viên', 0, 1, 0, 0, 1, 1, 1, 1, 0, 1);
INSERT INTO PHAN_QUYEN_NHOM VALUES('MEMBER', N'Thành viên', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

-- =============================================================================
-- 5. LOẠI CUỘC TRÒ CHUYỆN
-- =============================================================================
INSERT INTO LOAICTC VALUES('GROUP', N'Nhóm chat', 'N', N'Cuộc trò chuyện nhóm');
INSERT INTO LOAICTC VALUES('PRIVATE', N'Chat riêng tư', 'Y', N'Chat 1-1');
INSERT INTO LOAICTC VALUES('CHANNEL', N'Kênh thông báo', 'N', N'Kênh công khai');
INSERT INTO LOAICTC VALUES('BROADCAST', N'Phát sóng', 'N', N'Kênh phát sóng');
INSERT INTO LOAICTC VALUES('SUPPORT', N'Hỗ trợ', 'Y', N'Kênh hỗ trợ kỹ thuật');

-- =============================================================================
-- 6. LOẠI TIN NHẮN & TRẠNG THÁI
-- =============================================================================
INSERT INTO LOAITN VALUES('TEXT', N'Văn bản');
INSERT INTO LOAITN VALUES('IMAGE', N'Hình ảnh');
INSERT INTO LOAITN VALUES('VIDEO', N'Video');
INSERT INTO LOAITN VALUES('AUDIO', N'Âm thanh');
INSERT INTO LOAITN VALUES('FILE', N'Tệp đính kèm');
INSERT INTO LOAITN VALUES('LOCATION', N'Vị trí');
INSERT INTO LOAITN VALUES('ENCRYPTED', N'Tin mã hóa');
INSERT INTO LOAITN VALUES('SYSTEM', N'Thông báo hệ thống');

INSERT INTO TRANGTHAI VALUES('ACTIVE', N'Đang hoạt động');
INSERT INTO TRANGTHAI VALUES('DELETED', N'Đã xóa');
INSERT INTO TRANGTHAI VALUES('EDITED', N'Đã chỉnh sửa');
INSERT INTO TRANGTHAI VALUES('HIDDEN', N'Đã ẩn');
INSERT INTO TRANGTHAI VALUES('PENDING', N'Đang chờ duyệt');
INSERT INTO TRANGTHAI VALUES('RECALLED', N'Đã thu hồi');

-- =============================================================================
-- 7. TÀI KHOẢN MẪU (Password = 123, SHA256 hash, IS_OTP_VERIFIED = 1)
-- SHA256("123") = a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3
-- =============================================================================
-- Chủ dịch vụ - Clearance Level 5
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED, PROFILE_NAME)
VALUES('TK001', 'giamdoc', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'VT001', 5, 1, 'CHAT_ADMIN_PROFILE');

-- Quản trị viên - Clearance Level 4
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED, PROFILE_NAME)
VALUES('TK002', 'quantrivien', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'VT002', 4, 1, 'CHAT_ADMIN_PROFILE');

-- Phó Giám đốc - Clearance Level 4
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK003', 'phogiamdoc', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'VT003', 4, 1);

-- Trưởng phòng IT - Clearance Level 4
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK004', 'truongphongit', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'VT003', 4, 1);

-- Chuyên viên Kế toán - Clearance Level 3
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK005', 'chuyenvienkt', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'VT003', 3, 1);

-- Nhân viên Kinh doanh - Clearance Level 2
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK006', 'nhanvienkd', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'VT003', 2, 1);

-- Nhân viên IT - Clearance Level 3
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED)
VALUES('TK007', 'nhanvienit', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'VT003', 3, 1);

-- Thực tập sinh - Clearance Level 1
INSERT INTO TAIKHOAN (MATK, TENTK, PASSWORD_HASH, MAVAITRO, CLEARANCELEVEL, IS_OTP_VERIFIED, PROFILE_NAME)
VALUES('TK008', 'thuctapsinh', 'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', 'VT003', 1, 1, 'CHAT_INTERN_PROFILE');

-- =============================================================================
-- 8. THÔNG TIN NGƯỜI DÙNG
-- =============================================================================
INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT)
VALUES('TK001', 'PB001', 'CV001', N'Nguyễn Văn Minh', 'minh@company.com', '0901234567');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT)
VALUES('TK002', 'PB005', 'CV003', N'Trần Thị Hương', 'huong@company.com', '0902345678');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT)
VALUES('TK003', 'PB001', 'CV002', N'Lê Văn Tuấn', 'tuan@company.com', '0903456789');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT)
VALUES('TK004', 'PB005', 'CV003', N'Phạm Văn Đức', 'duc@company.com', '0904567890');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT)
VALUES('TK005', 'PB002', 'CV005', N'Hoàng Thị Lan', 'lan@company.com', '0905678901');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT)
VALUES('TK006', 'PB003', 'CV006', N'Võ Văn Nam', 'nam@company.com', '0906789012');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT)
VALUES('TK007', 'PB005', 'CV006', N'Nguyễn Văn Hùng', 'hung@company.com', '0907890123');

INSERT INTO NGUOIDUNG (MATK, MAPB, MACV, HOVATEN, EMAIL, SDT)
VALUES('TK008', 'PB005', 'CV007', N'Lê Thị Hoa', 'hoa@company.com', '0908901234');

-- =============================================================================
-- 9. CUỘC TRÒ CHUYỆN MẪU
-- =============================================================================
-- Nhóm Ban Giám Đốc (MIN_CLEARANCE = 4)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MIN_CLEARANCE, IS_ENCRYPTED)
VALUES('CTC_BGD_001', 'GROUP', N'Ban Giám Đốc', 'TK001', 'N', 'TK001', 4, 1);

INSERT INTO THANHVIEN VALUES('CTC_BGD_001', 'TK001', SYSTIMESTAMP, 'owner', 'OWNER', 0, 0, 0, NULL);
INSERT INTO THANHVIEN VALUES('CTC_BGD_001', 'TK002', SYSTIMESTAMP, 'admin', 'ADMIN', 0, 0, 0, NULL);
INSERT INTO THANHVIEN VALUES('CTC_BGD_001', 'TK003', SYSTIMESTAMP, 'member', 'MEMBER', 0, 0, 0, NULL);

-- Nhóm Phòng IT (MIN_CLEARANCE = 2)
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MIN_CLEARANCE)
VALUES('CTC_IT_001', 'GROUP', N'Phòng Công Nghệ', 'TK004', 'N', 'TK004', 2);

INSERT INTO THANHVIEN VALUES('CTC_IT_001', 'TK004', SYSTIMESTAMP, 'owner', 'OWNER', 0, 0, 0, NULL);
INSERT INTO THANHVIEN VALUES('CTC_IT_001', 'TK007', SYSTIMESTAMP, 'admin', 'ADMIN', 0, 0, 0, NULL);
INSERT INTO THANHVIEN VALUES('CTC_IT_001', 'TK008', SYSTIMESTAMP, 'member', 'MEMBER', 0, 0, 0, NULL);
INSERT INTO THANHVIEN VALUES('CTC_IT_001', 'TK002', SYSTIMESTAMP, 'member', 'MEMBER', 0, 0, 0, NULL);

-- Kênh thông báo công ty
INSERT INTO CUOCTROCHUYEN (MACTC, MALOAICTC, TENCTC, NGUOIQL, IS_PRIVATE, CREATED_BY, MIN_CLEARANCE)
VALUES('CTC_CHANNEL_001', 'CHANNEL', N'Thông Báo Công Ty', 'TK001', 'N', 'TK001', 1);

INSERT INTO THANHVIEN VALUES('CTC_CHANNEL_001', 'TK001', SYSTIMESTAMP, 'owner', 'OWNER', 0, 0, 0, NULL);
INSERT INTO THANHVIEN VALUES('CTC_CHANNEL_001', 'TK002', SYSTIMESTAMP, 'admin', 'ADMIN', 0, 0, 0, NULL);

-- =============================================================================
-- 10. TIN NHẮN MẪU (cần SET MAC CONTEXT trước)
-- =============================================================================
BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK001', 5); END;
/
INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_CHANNEL_001', 'TK001', 'TEXT', 'ACTIVE', N'Chào mừng tất cả đến với hệ thống chat nội bộ!', 1);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK001', 'TEXT', 'ACTIVE', N'[NỘI BỘ] Cuộc họp chiến lược Q4 vào thứ 6.', 4);

INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_BGD_001', 'TK001', 'TEXT', 'ACTIVE', N'[TỐI MẬT] Ngân sách dự án M&A: 50 tỷ đồng.', 5);

BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK004', 4); END;
/
INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK004', 'TEXT', 'ACTIVE', N'Team IT, tuần này focus vào security audit!', 2);

BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK007', 3); END;
/
INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK007', 'TEXT', 'ACTIVE', N'Em đã fix xong bug #1234. Anh Đức review giúp em!', 2);

BEGIN MAC_CTX_PKG.SET_USER_LEVEL('TK008', 1); END;
/
INSERT INTO TINNHAN (MACTC, MATK, MALOAITN, MATRANGTHAI, NOIDUNG, SECURITYLABEL)
VALUES('CTC_IT_001', 'TK008', 'TEXT', 'ACTIVE', N'Em đang học React và Node.js ạ.', 1);

BEGIN MAC_CTX_PKG.CLEAR_CONTEXT; END;
/

COMMIT;

-- =============================================================================
-- VERIFY
-- =============================================================================
SELECT 'Seeds inserted successfully!' AS STATUS FROM DUAL;
SELECT 'Tai khoan: ' || COUNT(*) AS STAT FROM TAIKHOAN;
SELECT 'Cuoc tro chuyen: ' || COUNT(*) AS STAT FROM CUOCTROCHUYEN;
SELECT 'Tin nhan: ' || COUNT(*) AS STAT FROM TINNHAN;
SELECT 'Policies: ' || COUNT(*) AS STAT FROM ADMIN_POLICY;

--------------------------------------------------------------------------------
-- CLEARANCE LEVELS (MAC - Bell-LaPadula):
-- Level 5: Giám đốc - TỐI MẬT - Xem tất cả
-- Level 4: Phó GĐ, Trưởng phòng, Admin - MẬT - Xem level 1-4
-- Level 3: Chuyên viên, NV IT - NỘI BỘ - Xem level 1-3
-- Level 2: Nhân viên thường - CÔNG KHAI NỘI BỘ - Xem level 1-2
-- Level 1: Thực tập sinh - CÔNG KHAI - Chỉ xem level 1
--------------------------------------------------------------------------------
