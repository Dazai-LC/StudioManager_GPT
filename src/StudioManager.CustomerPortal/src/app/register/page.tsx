'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
  User,
  Mail,
  Phone,
  Lock,
  ArrowRight,
  AlertCircle,
  CheckCircle2,
  Info,
  ShieldAlert,
} from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { PortalAuthLayout } from '@/components/PortalAuthLayout';
import { PasswordStrengthMeter } from '@/components/PasswordStrengthMeter';

export default function RegisterPage() {
  const router = useRouter();

  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    phoneNumber: '',
    password: '',
    confirmPassword: '',
  });

  const [error, setError] = useState<string | null>(null);
  const [successInfo, setSuccessInfo] = useState<{
    message: string;
    hasCounterConflict?: boolean;
    counterGuidance?: string;
  } | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccessInfo(null);

    // Validation
    if (!formData.fullName.trim()) {
      setError('Vui lòng nhập họ và tên của bạn.');
      return;
    }
    if (!formData.email.trim() || !formData.email.includes('@')) {
      setError('Vui lòng nhập địa chỉ email hợp lệ.');
      return;
    }
    if (!formData.phoneNumber.trim() || formData.phoneNumber.length < 10) {
      setError('Vui lòng nhập số điện thoại hợp lệ (tối thiểu 10 số).');
      return;
    }
    if (formData.password.length < 6) {
      setError('Mật khẩu phải có độ dài tối thiểu 6 ký tự.');
      return;
    }
    if (formData.password !== formData.confirmPassword) {
      setError('Mật khẩu xác nhận không khớp với mật khẩu đã nhập.');
      return;
    }

    setLoading(true);
    const result = await portalDataProvider.simulateRegister({
      fullName: formData.fullName,
      email: formData.email,
      phoneNumber: formData.phoneNumber,
      password: formData.password,
    });
    setLoading(false);

    if (!result.success) {
      setError(result.error || 'Đăng ký không thành công.');
      return;
    }

    if (result.conflictType === 'COUNTER_PROFILE_CONFLICT') {
      setSuccessInfo({
        message: 'Tài khoản Cổng Khách Hàng đã được tạo thành công!',
        hasCounterConflict: true,
        counterGuidance: result.existingCounterCustomer?.guidance,
      });
    } else {
      setSuccessInfo({
        message: 'Chúc mừng bạn đã tạo tài khoản Cổng Khách Hàng thành công!',
        hasCounterConflict: false,
      });
    }
  };

  // Demo helper buttons for rapid evaluation
  const fillNormalNewAccount = () => {
    setFormData({
      fullName: 'Phan Minh Hoàng',
      email: 'hoang.phan@example.com',
      phoneNumber: '0977112233',
      password: 'Password123@',
      confirmPassword: 'Password123@',
    });
    setError(null);
  };

  const fillCounterConflictAccount = () => {
    // Phone 0912345678 matches counter customer Tran Thi Binh in 03_SeedData.sql
    setFormData({
      fullName: 'Trần Thị Bình',
      email: 'binh.portal@example.com',
      phoneNumber: '0912345678',
      password: 'Password123@',
      confirmPassword: 'Password123@',
    });
    setError(null);
  };

  const fillDuplicateAccount = () => {
    // Phone 0901234567 matches pre-existing portal account
    setFormData({
      fullName: 'Nguyễn Văn An',
      email: 'khachhang.verified@atelierlumiere.vn',
      phoneNumber: '0901234567',
      password: 'Password123@',
      confirmPassword: 'Password123@',
    });
    setError(null);
  };

  return (
    <PortalAuthLayout
      title="Đăng Ký Thành Viên"
      subtitle="Tạo tài khoản Cổng Khách Hàng để quản lý lịch chụp, xem album và thanh toán cọc VietQR."
      bgAssetId="BG-AUTH-03"
      bgTitle="Chào đón thành viên mới"
      bgSubtitle="Trải nghiệm dịch vụ nhiếp ảnh cá nhân hóa với sự chăm sóc chu đáo từ đội ngũ cố vấn hình ảnh."
    >
      <div className="space-y-6">
        {/* Demo Quick-Fill Pill Bar */}
        <div className="p-3 bg-alabaster-100 rounded-atelier border border-stone-200/80">
          <span className="text-[11px] font-mono text-stone-500 uppercase block mb-1.5 font-semibold">
            Kịch bản thử nghiệm nhanh (DEC-03 Testing):
          </span>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={fillNormalNewAccount}
              className="px-2.5 py-1 text-[11px] font-medium bg-alabaster-50 hover:bg-stone-200 border border-stone-300 rounded text-stone-700 transition-colors"
            >
              1. Khách hàng mới hoàn toàn
            </button>
            <button
              type="button"
              onClick={fillCounterConflictAccount}
              className="px-2.5 py-1 text-[11px] font-medium bg-amber-50 hover:bg-amber-100 border border-amber-300 rounded text-amber-800 transition-colors"
            >
              2. Trùng SĐT khách tại quầy (DEC-03)
            </button>
            <button
              type="button"
              onClick={fillDuplicateAccount}
              className="px-2.5 py-1 text-[11px] font-medium bg-red-50 hover:bg-red-100 border border-red-300 rounded text-red-800 transition-colors"
            >
              3. Trùng tài khoản portal đã có
            </button>
          </div>
        </div>

        {/* Success Banner */}
        {successInfo && (
          <div className="p-4 rounded-atelier bg-emerald-50 border border-emerald-300 text-xs text-emerald-900 space-y-3">
            <div className="flex items-start gap-2.5">
              <CheckCircle2 className="h-5 w-5 text-emerald-600 shrink-0 mt-0.5" />
              <div>
                <p className="font-semibold text-sm">{successInfo.message}</p>
                <p className="mt-1 text-emerald-700">
                  Bạn đã đăng nhập tự động vào hệ thống Cổng Khách Hàng (In-memory mock session).
                </p>
              </div>
            </div>

            {successInfo.hasCounterConflict && (
              <div className="p-3 bg-amber-50/80 rounded border border-amber-200 text-amber-900 text-xs space-y-2">
                <div className="flex items-center gap-1.5 font-semibold">
                  <ShieldAlert className="h-4 w-4 text-amber-700" />
                  <span>Phát hiện hồ sơ tại quầy (Quy tắc DEC-03):</span>
                </div>
                <p className="text-amber-800 leading-relaxed">
                  {successInfo.counterGuidance}
                </p>
                <Link
                  href="/verify-identity"
                  className="inline-flex items-center gap-1 font-semibold text-terracotta-700 hover:text-terracotta-800 underline"
                >
                  <span>Chuyển sang bước Xác thực & Liên kết hồ sơ ngay</span>
                  <ArrowRight className="h-3.5 w-3.5" />
                </Link>
              </div>
            )}

            <div className="pt-2 flex items-center gap-3">
              <Link
                href="/packages"
                className="px-4 py-2 rounded-atelier bg-emerald-700 text-alabaster-50 font-medium text-xs hover:bg-emerald-800 transition-colors"
              >
                Khám phá các gói chụp ngay
              </Link>
              <Link
                href="/"
                className="px-4 py-2 rounded-atelier bg-alabaster-50 text-stone-700 font-medium text-xs border border-stone-300 hover:bg-stone-100 transition-colors"
              >
                Về Trang chủ
              </Link>
            </div>
          </div>
        )}

        {/* Error Alert */}
        {error && (
          <div className="p-3.5 rounded-atelier bg-red-50 border border-red-200 text-xs text-red-700 flex items-start gap-2.5">
            <AlertCircle className="h-4 w-4 text-red-600 shrink-0 mt-0.5" />
            <span>{error}</span>
          </div>
        )}

        {/* Form Fields */}
        {!successInfo && (
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Full Name */}
            <div>
              <label className="block text-xs font-medium text-stone-700 mb-1">
                Họ và tên <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <User className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  required
                  placeholder="Ví dụ: Nguyễn Văn An"
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
                />
              </div>
            </div>

            {/* Email & Phone grid */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium text-stone-700 mb-1">
                  Địa chỉ Email <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <Mail className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                  <input
                    type="email"
                    required
                    placeholder="name@example.com"
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-stone-700 mb-1">
                  Số điện thoại <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <Phone className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                  <input
                    type="tel"
                    required
                    placeholder="0901 234 567"
                    value={formData.phoneNumber}
                    onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                    className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500 font-mono"
                  />
                </div>
              </div>
            </div>

            {/* Password & Confirm */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium text-stone-700 mb-1">
                  Mật khẩu <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <Lock className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                  <input
                    type="password"
                    required
                    placeholder="Tối thiểu 8 ký tự"
                    value={formData.password}
                    onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                    className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-stone-700 mb-1">
                  Xác nhận mật khẩu <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <Lock className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                  <input
                    type="password"
                    required
                    placeholder="Nhập lại mật khẩu"
                    value={formData.confirmPassword}
                    onChange={(e) => setFormData({ ...formData, confirmPassword: e.target.value })}
                    className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
                  />
                </div>
              </div>
            </div>

            {/* Dynamic Password Strength Indicator */}
            <PasswordStrengthMeter password={formData.password} />

            {/* Policy checkbox disclaimer */}
            <div className="pt-2 text-[11px] text-stone-500 space-y-1">
              <p>
                Bằng việc nhấn Đăng ký, bạn đồng ý với Điều khoản Dịch vụ và Quy chế Bảo mật thông tin của Atelier Lumière.
              </p>
              <p className="text-stone-400 italic">
                * Lưu ý: Hồ sơ khách hàng tại quầy và tài khoản Cổng Khách Hàng được phân tách độc lập (Kiến trúc DEC-03).
              </p>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 px-4 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 font-medium text-xs transition-colors shadow-sm flex items-center justify-center gap-2 mt-4"
            >
              {loading ? (
                <div className="h-4 w-4 rounded-full border-2 border-alabaster-50 border-t-transparent animate-spin" />
              ) : (
                <>
                  <span>Hoàn tất đăng ký tài khoản</span>
                  <ArrowRight className="h-4 w-4" />
                </>
              )}
            </button>
          </form>
        )}

        {/* Bottom Switch to Login */}
        <div className="pt-4 border-t border-stone-200/80 text-center text-xs text-stone-600">
          <span>Đã có tài khoản Cổng Khách Hàng? </span>
          <Link
            href="/login"
            className="font-semibold text-terracotta-700 hover:text-terracotta-800 underline"
          >
            Đăng nhập ngay
          </Link>
        </div>
      </div>
    </PortalAuthLayout>
  );
}

