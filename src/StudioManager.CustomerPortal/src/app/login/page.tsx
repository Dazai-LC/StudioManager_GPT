'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Mail, Lock, ArrowRight, AlertCircle, CheckCircle2, ShieldCheck, KeyRound } from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { PortalAuthLayout } from '@/components/PortalAuthLayout';

export default function LoginPage() {
  const router = useRouter();

  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!identifier.trim()) {
      setError('Vui lòng nhập Email hoặc Số điện thoại đăng nhập.');
      return;
    }
    if (!password) {
      setError('Vui lòng nhập mật khẩu.');
      return;
    }

    setLoading(true);
    const result = await portalDataProvider.simulateLogin(identifier, password);
    setLoading(false);

    if (!result.success) {
      setError(result.error || 'Đăng nhập không thành công.');
      return;
    }

    // Success -> redirect to homepage
    router.push('/');
    router.refresh();
  };

  // Demo Fast Login buttons
  const quickFill = (demoId: string, demoPass: string = 'Password123@') => {
    setIdentifier(demoId);
    setPassword(demoPass);
    setError(null);
  };

  return (
    <PortalAuthLayout
      title="Đăng Nhập Cổng Khách Hàng"
      subtitle="Truy cập không gian quản lý lịch chụp, xem ảnh hậu kỳ và thanh toán cọc VietQR."
      bgAssetId="BG-AUTH-02"
      bgTitle="Chào mừng bạn quay lại"
      bgSubtitle="Đắm mình trong không gian nghệ thuật và tiếp tục hành trình lưu giữ khoảnh khắc quý giá."
    >
      <div className="space-y-6">
        {/* Fast Demo Accounts Picker (Crucial for Reviewer Experience) */}
        <div className="p-3.5 bg-alabaster-100 rounded-atelier border border-stone-200/90 space-y-2">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-stone-700">
            <KeyRound className="h-3.5 w-3.5 text-terracotta-600" />
            <span>Chọn nhanh tài khoản mô phỏng (Demo Presets):</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 pt-1">
            <button
              type="button"
              onClick={() => quickFill('khachhang.verified@atelierlumiere.vn')}
              className="p-2 text-left bg-alabaster-50 hover:bg-stone-200/80 rounded border border-stone-300 text-[11px] transition-colors"
            >
              <span className="font-semibold text-emerald-800 block">1. Đã liên kết (An)</span>
              <span className="text-stone-500 font-mono text-[10px] truncate block">KH000001 (Đã duyệt)</span>
            </button>

            <button
              type="button"
              onClick={() => quickFill('khachhang.new@atelierlumiere.vn')}
              className="p-2 text-left bg-alabaster-50 hover:bg-stone-200/80 rounded border border-stone-300 text-[11px] transition-colors"
            >
              <span className="font-semibold text-espresso-900 block">2. Khách mới (Phương)</span>
              <span className="text-stone-500 font-mono text-[10px] truncate block">Chưa liên kết quầy</span>
            </button>

            <button
              type="button"
              onClick={() => quickFill('khachhang.pending@atelierlumiere.vn')}
              className="p-2 text-left bg-alabaster-50 hover:bg-stone-200/80 rounded border border-stone-300 text-[11px] transition-colors"
            >
              <span className="font-semibold text-amber-800 block">3. Chờ duyệt (Bình)</span>
              <span className="text-stone-500 font-mono text-[10px] truncate block">Đang chờ xác minh</span>
            </button>
          </div>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="p-3.5 rounded-atelier bg-red-50 border border-red-200 text-xs text-red-700 flex items-start gap-2.5">
            <AlertCircle className="h-4 w-4 text-red-600 shrink-0 mt-0.5" />
            <span>{error}</span>
          </div>
        )}

        {/* Login Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-stone-700 mb-1">
              Email hoặc Số điện thoại <span className="text-red-500">*</span>
            </label>
            <div className="relative">
              <Mail className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                required
                placeholder="name@example.com hoặc 0901234567"
                value={identifier}
                onChange={(e) => setIdentifier(e.target.value)}
                className="w-full pl-9 pr-3 py-2.5 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
              />
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs font-medium text-stone-700">
                Mật khẩu <span className="text-red-500">*</span>
              </label>
              <Link
                href="/recovery"
                className="text-[11px] text-terracotta-700 hover:text-terracotta-800 transition-colors"
              >
                Quên mật khẩu?
              </Link>
            </div>
            <div className="relative">
              <Lock className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="password"
                required
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full pl-9 pr-3 py-2.5 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
              />
            </div>
          </div>

          {/* Remember me & Security info */}
          <div className="flex items-center justify-between text-xs text-stone-600 pt-1">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                className="rounded border-stone-300 text-terracotta-600 focus:ring-terracotta-500 h-3.5 w-3.5"
              />
              <span>Ghi nhớ đăng nhập trên thiết bị này</span>
            </label>
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
                <span>Đăng nhập Cổng Khách Hàng</span>
                <ArrowRight className="h-4 w-4" />
              </>
            )}
          </button>
        </form>

        {/* Switch to Register */}
        <div className="pt-4 border-t border-stone-200/80 text-center text-xs text-stone-600">
          <span>Chưa có tài khoản thành viên? </span>
          <Link
            href="/register"
            className="font-semibold text-terracotta-700 hover:text-terracotta-800 underline"
          >
            Tạo tài khoản mới
          </Link>
        </div>
      </div>
    </PortalAuthLayout>
  );
}

