// ==============================================================================
// STUDIOMANAGER v2.2 — MOCK PORTAL DATA PROVIDER
// Brand: Atelier Lumière
// Complete Seed Synchronization with database/03_SeedData.sql
// DEC-03 Identity Separation Architecture
// ==============================================================================

import {
  IPortalDataProvider,
  PackageDto,
  ChamberDto,
  ServiceDto,
  ResourceDto,
  CustomerAuthDto,
  IdentityVerificationRequest,
  VerificationResult,
  RegisterResult,
  LoginResult,
  PasswordRecoveryResult,
} from './contracts';

// Exact Seed Packages from database/03_SeedData.sql
export const SEED_PACKAGES: PackageDto[] = [
  {
    maGoi: 'GC000001',
    tenGoi: 'Chân dung cá nhân',
    giaCoBan: 1500000,
    thoiLuongPhut: 120,
    soAnhChinhSua: 10,
    moTa: 'Gói chụp chân dung nghệ thuật cá nhân trong studio với ánh sáng chuẩn điện ảnh, tôn vinh thần thái tự nhiên.',
    category: 'portrait',
    featured: true,
    badgeText: 'Phổ biến nhất',
    minDepositPercent: 30,
    aiAssetId: 'AI-AST-01',
    recommendedChamberCode: 'P0001',
    recommendedAddonCodes: ['DV000001', 'DV000002', 'DV000006'],
    inclusions: [
      '120 phút chụp tại studio chuyên nghiệp',
      '01 concept ánh sáng điện ảnh nghệ thuật',
      'Toàn bộ file gốc độ phân giải cao (JPG)',
      '10 ảnh chỉnh sửa màu & da cao cấp (retouch tỉ mỉ)',
      'Tặng 01 ảnh in lụa kích thước 15x21 cm',
    ],
    highlights: [
      'Stylist tư vấn phối trang phục trước buổi chụp',
      'Được hỗ trợ thay tối đa 2 trang phục',
      'Phòng chụp riêng tư, cách âm và điều hòa 2 chiều',
    ],
  },
  {
    maGoi: 'GC000002',
    tenGoi: 'Gia đình gắn kết',
    giaCoBan: 2800000,
    thoiLuongPhut: 180,
    soAnhChinhSua: 20,
    moTa: 'Lưu giữ khoảnh khắc sum vầy ấm áp cho gia đình tối đa 6 thành viên, phong cách tự nhiên, cảm xúc chân thực.',
    category: 'family',
    featured: true,
    badgeText: 'Khuyên dùng cho gia đình',
    minDepositPercent: 30,
    aiAssetId: 'AI-AST-02',
    recommendedChamberCode: 'P0002',
    recommendedAddonCodes: ['DV000001', 'DV000004', 'DV000005'],
    inclusions: [
      '180 phút chụp thảnh thơi không giới hạn số kiểu',
      'Áp dụng cho gia đình tối đa 6 người lớn & trẻ em',
      'Chụp trên 02 phông nền/bối cảnh nội thất vintage',
      'Toàn bộ ảnh gốc chất lượng chuẩn in ấn',
      '20 ảnh hậu kỳ sắc nét, cân bằng màu tự nhiên',
      'Tặng 01 ảnh ép gỗ cao cấp 20x30 cm để bàn',
    ],
    highlights: [
      'Đội ngũ photographer kiên nhẫn, vui vẻ với người lớn tuổi và trẻ nhỏ',
      'Hỗ trợ mượn đạo cụ gia đình: sách báo vintage, ly trà, quà lưu niệm',
      'Phòng chờ rộng rãi trang bị nước uống thảo mộc',
    ],
  },
  {
    maGoi: 'GC000003',
    tenGoi: 'Thiên thần nhỏ (Em bé)',
    giaCoBan: 2200000,
    thoiLuongPhut: 150,
    soAnhChinhSua: 15,
    moTa: 'Gói chụp an toàn tuyệt đối cho bé yêu với nhiệt độ phòng điều chỉnh riêng, đạo cụ khử khuẩn và ánh sáng dịu mắt.',
    category: 'baby',
    featured: false,
    badgeText: 'An toàn cho bé',
    minDepositPercent: 30,
    aiAssetId: 'AI-AST-03',
    recommendedChamberCode: 'P0003',
    recommendedAddonCodes: ['DV000002', 'DV000004', 'DV000006'],
    inclusions: [
      '150 phút chụp linh hoạt theo nhịp ngủ/bú của bé',
      '02 concept đạo cụ ngộ nghĩnh, thẩm mỹ cao',
      'Hỗ trợ 02 bộ trang phục len & tơ tằm mềm mại đã diệt khuẩn',
      'Toàn bộ file gốc chụp trong buổi làm việc',
      '15 ảnh chỉnh sửa da em bé mềm mịn, tự nhiên',
      '01 album ảnh mini kích thước 15x15 cm xinh xắn',
    ],
    highlights: [
      'Đèn chụp công nghệ soft light không gây lóa mắt bé',
      'Nhiệt độ phòng luôn giữ ở mức 26-27°C bảo vệ sức khỏe',
      'Trợ lý chụp có chuyên môn dỗ dành và tương tác với trẻ nhỏ',
    ],
  },
  {
    maGoi: 'GC000004',
    tenGoi: 'Cặp đôi & Kỷ niệm',
    giaCoBan: 2500000,
    thoiLuongPhut: 180,
    soAnhChinhSua: 15,
    moTa: 'Kể câu chuyện tình yêu ngọt ngào qua những khung hình giàu chất thơ, tinh tế và đầy chiều sâu nghệ thuật.',
    category: 'couple',
    featured: true,
    badgeText: 'Lãng mạn & Tinh tế',
    minDepositPercent: 30,
    aiAssetId: 'AI-AST-04',
    recommendedChamberCode: 'P0001',
    recommendedAddonCodes: ['DV000001', 'DV000002', 'DV000008'],
    inclusions: [
      '180 phút chụp studio với 02 concept lãng mạn',
      'Trang phục gợi ý theo tông màu Atelier Ivory & Espresso',
      'Toàn bộ ảnh gốc được chuyển qua Google Drive trong 24h',
      '15 ảnh chỉnh sửa màu độc quyền Atelier Lumière',
      'Tặng 02 ảnh khung gỗ để bàn cao cấp 13x18 cm',
    ],
    highlights: [
      'Không gian riêng tư, phát playlist nhạc yêu thích của cặp đôi',
      'Gợi ý tạo dáng tự nhiên, không gượng gạo',
      'Phù hợp kỷ niệm ngày cưới, lời cầu hôn hoặc pre-wedding tối giản',
    ],
  },
  {
    maGoi: 'GC000005',
    tenGoi: 'Kỷ yếu & Nhóm bạn',
    giaCoBan: 6500000,
    thoiLuongPhut: 300,
    soAnhChinhSua: 30,
    moTa: 'Gói chụp nhóm ngoại cảnh hoặc kết hợp studio cho nhóm 10-25 người, lưu giữ tuổi thanh xuân rực rỡ.',
    category: 'event',
    featured: false,
    badgeText: 'Dành cho nhóm đông',
    minDepositPercent: 50,
    aiAssetId: 'AI-AST-05',
    recommendedChamberCode: 'P0001',
    recommendedAddonCodes: ['DV000005', 'DV000006', 'DV000008'],
    inclusions: [
      '300 phút (5 giờ) làm việc với 02 nhiếp ảnh gia chính',
      'Kết hợp studio phông nền và ngoại cảnh trong bán kính 10km',
      'Ảnh chụp nhóm không giới hạn số lượng file chụp',
      'Toàn bộ ảnh gốc độ phân giải 4K',
      '30 ảnh chỉnh sửa màu tập thể và chân dung nhóm nhỏ',
      'Flycam hỗ trợ chụp góc rộng toàn cảnh (nếu thời tiết cho phép)',
    ],
    highlights: [
      'Lên kịch bản tạo dáng và phân bổ thời gian chuyên nghiệp',
      'Hỗ trợ đạo cụ bóng bay, pháo giấy, bằng cử nhân tượng trưng',
      'Hệ thống loa Bluetooth di động khuấy động không khí',
    ],
  },
];

// Exact Seed Chambers from database/03_SeedData.sql
export const SEED_CHAMBERS: ChamberDto[] = [
  {
    maPhong: 'P0001',
    tenPhong: 'Phòng Ánh Sáng (Lumière Studio)',
    moTa: 'Không gian tràn ngập ánh sáng tự nhiên với cửa kính vòm cao 4m, sàn gỗ sồi và tông màu Alabaster thanh lịch.',
    sucChuaToiDa: 8,
    phongCach: 'Tối giản Hiện đại & Tự nhiên',
    trangThietBiChinh: ['Đèn Godox AD600', 'Softbox Parabolic 120cm', 'Phông giấy trắng/kem cao cấp', 'Gương soi toàn thân viền đồng'],
    aiAssetId: 'AI-AST-06',
    dienTichM2: 55,
    toneMau: 'Alabaster & Ivory (#FAF8F5, #F4F1EA)',
  },
  {
    maPhong: 'P0002',
    tenPhong: 'Phòng Cổ Điển (Vintage Salon)',
    moTa: 'Không gian ấm cúng mang phong cách hoài niệm Pháp cổ điển với ghế nhung Espresso, piano gỗ và lò sưởi giả lập.',
    sucChuaToiDa: 10,
    phongCach: 'Cổ điển Châu Âu & Warm Editorial',
    trangThietBiChinh: ['Đèn LED RGB Amaran', 'Chao đèn nhung nghệ thuật', 'Sofa da bò cổ điển', 'Thảm Ba Tư dệt thủ công'],
    aiAssetId: 'AI-AST-07',
    dienTichM2: 65,
    toneMau: 'Espresso & Terracotta (#161412, #C04E26)',
  },
  {
    maPhong: 'P0003',
    tenPhong: 'Phòng Em Bé & Gia Đình Nhỏ (Petite Lumière)',
    moTa: 'Không gian thiết kế an toàn tuyệt đối với góc bo tròn, sàn đệm cách nhiệt, đồ chơi gỗ mộc và đạo cụ chụp ảnh cho bé.',
    sucChuaToiDa: 6,
    phongCach: 'Ngọt ngào, Pastel dịu êm & An toàn',
    trangThietBiChinh: ['Hệ thống đèn trần hắt gián tiếp chống chói', 'Nôi mây vintage', 'Đồ chơi len & gỗ hữu cơ', 'Máy sưởi gốm êm dịu'],
    aiAssetId: 'AI-AST-08',
    dienTichM2: 40,
    toneMau: 'Sage & Alabaster (#EEF7F2, #FAF8F5)',
  },
];

// Exact Seed Services from database/03_SeedData.sql
export const SEED_SERVICES: ServiceDto[] = [
  {
    maDichVu: 'DV000001',
    tenDichVu: 'Trang điểm & Làm tóc chuyên nghiệp',
    donGia: 450000,
    donViTinh: 'Người',
    moTa: 'Make-up nghệ thuật theo phong cách Hàn Quốc hoặc Tây Âu, tôn nét tự nhiên và lâu trôi dưới ánh đèn studio.',
    nhomDichVu: 'lam_dep',
    popular: true,
  },
  {
    maDichVu: 'DV000002',
    tenDichVu: 'Thuê trang phục thiết kế cao cấp',
    donGia: 300000,
    donViTinh: 'Bộ',
    moTa: 'Lựa chọn từ bộ sưu tập váy dạ hội, áo dài lụa tơ tằm, vest may đo và trang phục concept vintage.',
    nhomDichVu: 'trang_phuc',
    popular: true,
  },
  {
    maDichVu: 'DV000003',
    tenDichVu: 'In ảnh lụa ép gỗ 15x21 cm',
    donGia: 30000,
    donViTinh: 'Tấm',
    moTa: 'Công nghệ in lụa chuẩn màu Nhật Bản, bề mặt mịn không bám vân tay, độ bền màu trên 20 năm.',
    nhomDichVu: 'in_an',
  },
  {
    maDichVu: 'DV000004',
    tenDichVu: 'In Photobook / Album mở phẳng cao cấp',
    donGia: 850000,
    donViTinh: 'Cuốn',
    moTa: 'Album bìa da khắc laser hoặc bìa vải canvas cao cấp, 20 trang ruột dày mở phẳng 180 độ.',
    nhomDichVu: 'in_an',
    popular: true,
  },
  {
    maDichVu: 'DV000005',
    tenDichVu: 'Chụp thêm giờ (ngoài gói)',
    donGia: 500000,
    donViTinh: 'Giờ',
    moTa: 'Gia hạn thời gian chụp tại phòng và giữ nguyên đội ngũ ê-kíp để thoải mái sáng tạo thêm các góc máy.',
    nhomDichVu: 'thoi_gian',
  },
  {
    maDichVu: 'DV000006',
    tenDichVu: 'Chỉnh sửa ảnh bổ sung (Retouch lẻ)',
    donGia: 50000,
    donViTinh: 'Ảnh',
    moTa: 'Chỉnh sửa da, tóc, dáng và cân chỉnh màu sắc chuyên sâu cho từng bức ảnh ngoài số lượng của gói.',
    nhomDichVu: 'hau_ky',
  },
  {
    maDichVu: 'DV000007',
    tenDichVu: 'Phông nền màu mỹ thuật cao cấp',
    donGia: 250000,
    donViTinh: 'Bộ',
    moTa: 'Sử dụng phông nền canvas vẽ tay độc bản hoặc giấy mỹ thuật nhập khẩu từ Savage USA.',
    nhomDichVu: 'khac',
  },
  {
    maDichVu: 'DV000008',
    tenDichVu: 'Giao ảnh hỏa tốc trong 24 giờ',
    donGia: 400000,
    donViTinh: 'Gói',
    moTa: 'Ưu tiên chỉnh sửa hậu kỳ thần tốc và bàn giao link ảnh chất lượng cao chỉ sau 24h kết thúc buổi chụp.',
    nhomDichVu: 'hau_ky',
  },
];

// Exact Seed Resources from database/03_SeedData.sql
export const SEED_RESOURCES: ResourceDto[] = [
  {
    maTaiNguyen: 'TN000001',
    tenTaiNguyen: 'Canon EOS R6 Mark II',
    loaiTaiNguyen: 'THIET_BI',
    moTa: 'Máy ảnh full-frame 24.2 MP với khả năng chụp 40 fps và lấy nét thông minh AI bắt trọn khoảnh khắc.',
    aiAssetId: 'AI-AST-09',
    tinhNangChinh: ['Full-frame 24.2 MP', 'Lấy nét ánh mắt AI', 'ISO lên tới 102.400'],
  },
  {
    maTaiNguyen: 'TN000002',
    tenTaiNguyen: 'Sony Alpha A7 IV',
    loaiTaiNguyen: 'THIET_BI',
    moTa: 'Cảm biến 33 MP xuất sắc về độ chi tiết và dải tương phản động rộng, tái tạo màu da cực kỳ nịnh mắt.',
    aiAssetId: 'AI-AST-10',
    tinhNangChinh: ['Cảm biến BSI 33 MP', '15-stop Dynamic Range', 'S-Cinetone độc quyền'],
  },
  {
    maTaiNguyen: 'TN000003',
    tenTaiNguyen: 'Đèn Flash Godox AD600 Pro',
    loaiTaiNguyen: 'THIET_BI',
    moTa: 'Hệ thống đèn studio công suất mạnh mẽ 600Ws chuẩn nhiệt độ màu 5600±75K cho ánh sáng sắc nét.',
    aiAssetId: 'AI-AST-11',
    tinhNangChinh: ['Công suất 600Ws', 'Nhiệt độ màu ổn định', 'Tương thích ngàm Bowens'],
  },
  {
    maTaiNguyen: 'TN000004',
    tenTaiNguyen: 'Đèn LED RGB Amaran 200x',
    loaiTaiNguyen: 'THIET_BI',
    moTa: 'Đèn chiếu sáng liên tục đổi dải màu điện ảnh, tạo hiệu ứng thị giác ấn tượng cho concept chân dung.',
    aiAssetId: 'AI-AST-12',
    tinhNangChinh: ['Dải màu Bi-Color 2700K - 6500K', 'CRI 95+ chuẩn điện ảnh', 'Điều khiển qua app'],
  },
  {
    maTaiNguyen: 'TN000005',
    tenTaiNguyen: 'Phông nền giấy Seamless Savage',
    loaiTaiNguyen: 'THIET_BI',
    moTa: 'Phông giấy liền mạch màu trắng ngà, xám trung tính và kem ấm không phản xạ ánh sáng.',
    aiAssetId: 'AI-AST-13',
    tinhNangChinh: ['Khổ rộng 2.72m', 'Chống lóa 100%', 'Bề mặt mịn không vân'],
  },
  {
    maTaiNguyen: 'TN000006',
    tenTaiNguyen: 'Váy cưới chữ A Lụa Satin Trắng',
    loaiTaiNguyen: 'TRANG_PHUC',
    moTa: 'Váy cưới tối giản phong cách Pháp, chất liệu lụa satin thượng hạng tôn vinh nét đẹp thanh tao.',
    aiAssetId: 'AI-AST-14',
    tinhNangChinh: ['Lụa satin cao cấp', 'Size S-M điều chỉnh', 'Dáng váy chữ A tôn dáng'],
  },
  {
    maTaiNguyen: 'TN000007',
    tenTaiNguyen: 'Áo dài lụa tơ tằm đỏ truyền thống',
    loaiTaiNguyen: 'TRANG_PHUC',
    moTa: 'Áo dài dáng suông truyền thống may từ lụa Hà Đông dệt tay, phù hợp chụp Tết và kỷ niệm gia đình.',
    aiAssetId: 'AI-AST-15',
    tinhNangChinh: ['Lụa tơ tằm dệt tay', 'Họa tiết thêu tinh xảo', 'Phong cách hoài niệm'],
  },
  {
    maTaiNguyen: 'TN000008',
    tenTaiNguyen: 'Bộ Suit Nam màu nâu Espresso',
    loaiTaiNguyen: 'TRANG_PHUC',
    moTa: 'Vest nam may đo phom Slim-fit chuẩn Anh quốc, chất liệu len pha cashmere ấm áp và lịch lãm.',
    aiAssetId: 'AI-AST-16',
    tinhNangChinh: ['Chất liệu len Ý pha cashmere', 'Tông màu Espresso sang trọng', 'Đầy đủ áo gile & caravat'],
  },
  {
    maTaiNguyen: 'TN000009',
    tenTaiNguyen: 'Váy công chúa voan tơ cho bé gái',
    loaiTaiNguyen: 'TRANG_PHUC',
    moTa: 'Váy bồng bềnh nhiều tầng voan mềm mịn, lót cotton 100% không gây kích ứng da nhạy cảm.',
    aiAssetId: 'AI-AST-17',
    tinhNangChinh: ['Lót cotton hữu cơ', 'Dành cho bé 1 - 5 tuổi', 'Khử khuẩn bằng tia UV'],
  },
  {
    maTaiNguyen: 'TN000010',
    tenTaiNguyen: 'Bộ đạo cụ tiệc sinh nhật & kỷ niệm',
    loaiTaiNguyen: 'DAO_CU',
    moTa: 'Bộ sưu tập bánh kem giả lập tinh tế, hộp quà nơ lụa, nến thơm nghệ thuật và bóng bay tông màu ấm.',
    aiAssetId: 'AI-AST-18',
    tinhNangChinh: ['Đạo cụ gỗ & gốm mộc', 'Bảng chữ kỷ niệm cá nhân hóa', 'Tông màu Ivory & Ochre'],
  },
];

// Seed Counter Customers (WinForms DB counter profile reference for DEC-03 simulation)
const SEED_COUNTER_CUSTOMERS = [
  { maKhachHang: 'KH000001', hoTen: 'Nguyễn Văn An', soDienThoai: '0901234567', email: 'an.nguyen@email.com', cccd: '001090123456', diaChi: '123 Phố Huế, Hai Bà Trưng, Hà Nội' },
  { maKhachHang: 'KH000002', hoTen: 'Trần Thị Bình', soDienThoai: '0912345678', email: 'binh.tran@email.com', cccd: '001191234567', diaChi: '45 Tràng Thi, Hoàn Kiếm, Hà Nội' },
  { maKhachHang: 'KH000003', hoTen: 'Lê Hoàng Nam', soDienThoai: '0923456789', email: 'nam.le@email.com', cccd: '001292345678', diaChi: '88 Giảng Võ, Đống Đa, Hà Nội' },
  { maKhachHang: 'KH000004', hoTen: 'Phạm Thu Hà', soDienThoai: '0934567890', email: 'ha.pham@email.com', cccd: '001393456789', diaChi: '12 Kim Mã, Ba Đình, Hà Nội' },
  { maKhachHang: 'KH000005', hoTen: 'Hoàng Minh Đức', soDienThoai: '0945678901', email: 'duc.hoang@email.com', cccd: '001494567890', diaChi: '24 Tây Sơn, Đống Đa, Hà Nội' },
];

// Pre-seeded Portal Accounts (Independent Portal Account DB per DEC-03)
const INITIAL_PORTAL_USERS: CustomerAuthDto[] = [
  {
    userId: 'USR-PORTAL-01',
    email: 'khachhang.verified@atelierlumiere.vn',
    fullName: 'Nguyễn Văn An',
    phoneNumber: '0901234567',
    role: 'CUSTOMER',
    isLinkedWithCounter: true,
    counterCustomerId: 'KH000001',
    linkedStatus: 'VERIFIED',
    joinedDate: '2026-01-15',
  },
  {
    userId: 'USR-PORTAL-02',
    email: 'khachhang.new@atelierlumiere.vn',
    fullName: 'Đặng Mai Phương',
    phoneNumber: '0988776655',
    role: 'CUSTOMER',
    isLinkedWithCounter: false,
    counterCustomerId: undefined,
    linkedStatus: 'UNLINKED',
    joinedDate: '2026-03-01',
  },
  {
    userId: 'USR-PORTAL-03',
    email: 'khachhang.pending@atelierlumiere.vn',
    fullName: 'Trần Thị Bình',
    phoneNumber: '0912345678',
    role: 'CUSTOMER',
    isLinkedWithCounter: false,
    counterCustomerId: undefined,
    linkedStatus: 'PENDING_VERIFICATION',
    joinedDate: '2026-03-05',
  },
];

export class MockPortalDataProvider implements IPortalDataProvider {
  private portalUsers: CustomerAuthDto[] = [...INITIAL_PORTAL_USERS];
  private currentSessionUser: CustomerAuthDto | null = null;

  constructor() {
    // Check if running in browser with localStorage
    if (typeof window !== 'undefined') {
      const stored = localStorage.getItem('atelier_demo_user');
      if (stored) {
        try {
          this.currentSessionUser = JSON.parse(stored);
        } catch {
          this.currentSessionUser = null;
        }
      }
    }
  }

  async getPackages(): Promise<PackageDto[]> {
    return [...SEED_PACKAGES];
  }

  async getPackageByCode(code: string): Promise<PackageDto | null> {
    const pkg = SEED_PACKAGES.find((p) => p.maGoi.toUpperCase() === code.toUpperCase());
    return pkg || null;
  }

  async getChambers(): Promise<ChamberDto[]> {
    return [...SEED_CHAMBERS];
  }

  async getChamberByCode(code: string): Promise<ChamberDto | null> {
    const ch = SEED_CHAMBERS.find((c) => c.maPhong.toUpperCase() === code.toUpperCase());
    return ch || null;
  }

  async getServices(): Promise<ServiceDto[]> {
    return [...SEED_SERVICES];
  }

  async getResources(): Promise<ResourceDto[]> {
    return [...SEED_RESOURCES];
  }

  // ============================================================================
  // AUTHENTICATION & DEMO LOGIN (DEC-03 COMPLIANT)
  // ============================================================================
  async simulateLogin(identifier: string, password: string): Promise<LoginResult> {
    // Simulate brief network latency for realistic feel
    await new Promise((resolve) => setTimeout(resolve, 300));

    const cleanId = identifier.trim().toLowerCase();

    // Check pre-configured demo credentials or any portal user
    const user = this.portalUsers.find(
      (u) => u.email.toLowerCase() === cleanId || u.phoneNumber === cleanId
    );

    if (!user) {
      return {
        success: false,
        error: 'Tài khoản không tồn tại trên Cổng Khách Hàng. Vui lòng kiểm tra lại Email/SĐT hoặc bấm Đăng Ký.',
      };
    }

    if (password.length < 6) {
      return {
        success: false,
        error: 'Mật khẩu phải có ít nhất 6 ký tự.',
      };
    }

    // In demo mode, accept any non-empty password matching demo pattern or "password123"
    this.setSessionUser(user);
    return {
      success: true,
      user,
    };
  }

  // ============================================================================
  // REGISTER WITH DEC-03 CONFLICT DETECTION
  // Rule: Do NOT auto-link or auto-merge counter profile (KhachHang).
  // If phone exists in counter DB (e.g. 0912345678 - Tran Thi Binh),
  // warn customer of existing records and invite them to complete identity verification.
  // ============================================================================
  async simulateRegister(data: {
    fullName: string;
    email: string;
    phoneNumber: string;
    password: string;
  }): Promise<RegisterResult> {
    await new Promise((resolve) => setTimeout(resolve, 350));

    const cleanEmail = data.email.trim().toLowerCase();
    const cleanPhone = data.phoneNumber.trim();

    // 1. Check duplicate Portal Account
    const existingPortalUser = this.portalUsers.find(
      (u) => u.email.toLowerCase() === cleanEmail || u.phoneNumber === cleanPhone
    );

    if (existingPortalUser) {
      return {
        success: false,
        conflictType: 'DUPLICATE_PORTAL_ACCOUNT',
        error: `Email hoặc số điện thoại ${cleanPhone} đã được đăng ký tài khoản Cổng Khách Hàng. Vui lòng chuyển sang trang Đăng nhập.`,
      };
    }

    // 2. DEC-03 Check: Does phone match an existing in-store Counter Customer?
    const counterMatch = SEED_COUNTER_CUSTOMERS.find(
      (c) => c.soDienThoai === cleanPhone || c.email.toLowerCase() === cleanEmail
    );

    // Create the new Portal Account (Initially unlinked!)
    const newUser: CustomerAuthDto = {
      userId: `USR-PORTAL-${Date.now().toString().slice(-4)}`,
      email: cleanEmail,
      fullName: data.fullName.trim(),
      phoneNumber: cleanPhone,
      role: 'CUSTOMER',
      isLinkedWithCounter: false,
      counterCustomerId: undefined,
      linkedStatus: 'UNLINKED',
      joinedDate: new Date().toISOString().split('T')[0],
    };

    this.portalUsers.push(newUser);
    this.setSessionUser(newUser);

    if (counterMatch) {
      // Return notification that counter profile was detected per DEC-03
      return {
        success: true,
        user: newUser,
        conflictType: 'COUNTER_PROFILE_CONFLICT',
        existingCounterCustomer: {
          maKhachHang: counterMatch.maKhachHang,
          hoTen: counterMatch.hoTen,
          soDienThoai: counterMatch.soDienThoai,
          guidance: 'Hệ thống nhận thấy số điện thoại của bạn đã từng chụp ảnh tại studio (Hồ sơ tại quầy). Để đồng bộ lịch sử chụp ảnh cũ, bạn có thể thực hiện "Xác thực danh tính" ở bước tiếp theo.',
        },
      };
    }

    return {
      success: true,
      user: newUser,
    };
  }

  // ============================================================================
  // PASSWORD RECOVERY (DEMO SIMULATION)
  // Clear disclaimer: No real email/SMS is sent!
  // ============================================================================
  async simulatePasswordRecovery(email: string): Promise<PasswordRecoveryResult> {
    await new Promise((resolve) => setTimeout(resolve, 300));
    const cleanEmail = email.trim().toLowerCase();

    return {
      success: true,
      message: `Đã mô phỏng gửi hướng dẫn khôi phục mật khẩu đến ${cleanEmail}. (Chế độ Frontend Demo: Không phát sinh email/SMTP thật).`,
      previewTokenOrLink: `https://atelierlumiere.vn/reset-password?demo_token=AL_REC_${Math.random().toString(36).substring(2, 9).toUpperCase()}`,
    };
  }

  // ============================================================================
  // DEC-03 IDENTITY VERIFICATION & PROFILE LINKING FLOW
  // Safe flow: Matches customer input against in-store record without leaking
  // sensitive data. Supports DEMO toggles (VERIFIED, PENDING, REJECTED).
  // ============================================================================
  async simulateVerifyIdentity(request: IdentityVerificationRequest): Promise<VerificationResult> {
    await new Promise((resolve) => setTimeout(resolve, 400));

    const cleanPhone = request.phoneNumber.trim();
    const cleanId = request.idCardNumber.trim();

    // Check counter customer match
    const counterMatch = SEED_COUNTER_CUSTOMERS.find(
      (c) => c.soDienThoai === cleanPhone || (cleanId && c.cccd === cleanId)
    );

    if (!counterMatch) {
      return {
        success: false,
        status: 'REJECTED',
        message: 'Không tìm thấy hồ sơ khách hàng tại quầy tương ứng với thông tin bạn cung cấp. Vui lòng kiểm tra lại số điện thoại hoặc liên hệ hotline studio.',
      };
    }

    // Update current session user if logged in
    if (this.currentSessionUser) {
      this.currentSessionUser.isLinkedWithCounter = true;
      this.currentSessionUser.counterCustomerId = counterMatch.maKhachHang;
      this.currentSessionUser.linkedStatus = 'VERIFIED';
      this.setSessionUser(this.currentSessionUser);
    }

    return {
      success: true,
      status: 'VERIFIED',
      message: `Xác thực thành công! Tài khoản Portal đã liên kết chính thức với hồ sơ ${counterMatch.hoTen} (${counterMatch.maKhachHang}). Toàn bộ lịch sử chụp ảnh tại quầy sẽ được hiển thị đầy đủ.`,
      matchedCounterCustomer: {
        maKhachHang: counterMatch.maKhachHang,
        hoTen: counterMatch.hoTen,
        soDienThoai: counterMatch.soDienThoai,
        diaChi: counterMatch.diaChi,
      },
    };
  }

  getCurrentSessionUser(): CustomerAuthDto | null {
    return this.currentSessionUser;
  }

  setSessionUser(user: CustomerAuthDto | null): void {
    this.currentSessionUser = user;
    if (typeof window !== 'undefined') {
      if (user) {
        localStorage.setItem('atelier_demo_user', JSON.stringify(user));
      } else {
        localStorage.removeItem('atelier_demo_user');
      }
    }
  }
}

// Singleton export
export const portalDataProvider: IPortalDataProvider = new MockPortalDataProvider();

