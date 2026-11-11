// ==============================================================================
// STUDIOMANAGER v2.2 — CUSTOMER PORTAL CONTRACTS & DATA INTERFACES
// Brand: Atelier Lumière
// Approved Architecture: Presentation -> IPortalDataProvider -> MockPortalDataProvider
// Pure Frontend Isolation: Zero DB, Zero Backend Network Dependency
// ==============================================================================

export type PackageCategory = 'all' | 'portrait' | 'family' | 'baby' | 'couple' | 'event';

export interface PackageDto {
  maGoi: string; // e.g. "GC000001"
  tenGoi: string;
  giaCoBan: number;
  thoiLuongPhut: number;
  soAnhChinhSua: number;
  moTa: string;
  category: PackageCategory;
  featured: boolean;
  inclusions: string[];
  highlights: string[];
  recommendedChamberCode: string;
  recommendedAddonCodes: string[];
  minDepositPercent: number; // e.g. 30%
  aiAssetId: string; // e.g. "AI-AST-01"
  badgeText?: string;
}

export interface ChamberDto {
  maPhong: string; // e.g. "P0001"
  tenPhong: string;
  moTa: string;
  sucChuaToiDa: number;
  phongCach: string;
  trangThietBiChinh: string[];
  aiAssetId: string; // e.g. "AI-AST-06"
  dienTichM2?: number;
  toneMau?: string;
}

export type ServiceGroup = 'lam_dep' | 'trang_phuc' | 'in_an' | 'hau_ky' | 'thoi_gian' | 'khac';

export interface ServiceDto {
  maDichVu: string; // e.g. "DV000001"
  tenDichVu: string;
  donGia: number;
  donViTinh: string; // e.g. "Người", "Bộ", "Cuốn"
  moTa: string;
  nhomDichVu: ServiceGroup;
  popular?: boolean;
}

export type ResourceType = 'THIET_BI' | 'TRANG_PHUC' | 'DAO_CU';

export interface ResourceDto {
  maTaiNguyen: string; // e.g. "TN000001"
  tenTaiNguyen: string;
  loaiTaiNguyen: ResourceType;
  moTa: string;
  aiAssetId: string;
  tinhNangChinh?: string[];
}

export type LinkedIdentityStatus = 'UNLINKED' | 'PENDING_VERIFICATION' | 'VERIFIED' | 'REJECTED';

export interface CustomerAuthDto {
  userId: string;
  email: string;
  fullName: string;
  phoneNumber: string;
  avatarUrl?: string;
  role: 'CUSTOMER';
  isLinkedWithCounter: boolean;
  counterCustomerId?: string; // e.g. "KH000001"
  linkedStatus: LinkedIdentityStatus;
  joinedDate: string;
}

export interface IdentityVerificationRequest {
  fullName: string;
  phoneNumber: string;
  idCardNumber: string;
  recentBookingCode?: string;
  verificationNote?: string;
}

export interface VerificationResult {
  success: boolean;
  status: LinkedIdentityStatus;
  message: string;
  matchedCounterCustomer?: {
    maKhachHang: string;
    hoTen: string;
    soDienThoai: string;
    diaChi?: string;
  };
}

export interface RegisterResult {
  success: boolean;
  user?: CustomerAuthDto;
  error?: string;
  conflictType?: 'DUPLICATE_PORTAL_ACCOUNT' | 'COUNTER_PROFILE_CONFLICT';
  existingCounterCustomer?: {
    maKhachHang: string;
    hoTen: string;
    soDienThoai: string;
    guidance: string;
  };
}

export interface LoginResult {
  success: boolean;
  user?: CustomerAuthDto;
  error?: string;
}

export interface PasswordRecoveryResult {
  success: boolean;
  message: string;
  previewTokenOrLink?: string;
}

export interface IPortalDataProvider {
  getPackages(): Promise<PackageDto[]>;
  getPackageByCode(code: string): Promise<PackageDto | null>;
  getChambers(): Promise<ChamberDto[]>;
  getChamberByCode(code: string): Promise<ChamberDto | null>;
  getServices(): Promise<ServiceDto[]>;
  getResources(): Promise<ResourceDto[]>;
  
  // Authentication & Identity Simulation (DEC-03 compliant)
  simulateLogin(identifier: string, password: string): Promise<LoginResult>;
  simulateRegister(data: {
    fullName: string;
    email: string;
    phoneNumber: string;
    password: string;
  }): Promise<RegisterResult>;
  simulatePasswordRecovery(email: string): Promise<PasswordRecoveryResult>;
  simulateVerifyIdentity(request: IdentityVerificationRequest): Promise<VerificationResult>;
  
  // Session Simulation Helpers
  getCurrentSessionUser(): CustomerAuthDto | null;
  setSessionUser(user: CustomerAuthDto | null): void;
}

