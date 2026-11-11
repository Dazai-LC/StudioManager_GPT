'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { Mail, ArrowRight, ArrowLeft, CheckCircle2, AlertCircle, Sparkles, Key } from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { PortalAuthLayout } from '@/components/PortalAuthLayout';

export default function RecoveryPage() {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<{
    message: string;
    previewLink?: string;
  } | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setResult(null);

    if (!email.trim() || !email.includes('@')) {
      setError('Vui lòng nhập địa chỉ email hợp lệ.');
      return;
    }

    setLoading(true);
    const res = await portalDataProvider.simulatePasswordRecovery(email);
    setLoading(false);

    setResult({
      message: res.message,
      previewLink: res.previewTokenOrLink,
    });
  };

  return (
    <PortalAuthLayout
      title="Khôi Phục Mật Khẩu"
      subtitle="Nhập email đã đăng ký để nhận liên kết thiết lập lại mật khẩu an toàn."
      bgAssetId="BG-AUTH-04"
      bgTitle="Hỗ trợ bảo mật tài khoản"
      bgSubtitle="Đảm bảo quyền riêng tư và truy cập liền mạch vào bộ sưu tập ảnh cá nhân của bạn."
    >
      <div className="space-y-6">
        {/* Result message */}
        {result ? (
          <div className="p-4 rounded-atelier bg-emerald-50 border border-emerald-300 text-xs text-emerald-900 space-y-3">
            <div className="flex items-start gap-2.5">
              <CheckCircle2 className="h-5 w-5 text-emerald-600 shrink-0 mt-0.5" />
              <div>
                <p className="font-semibold text-sm">Yêu cầu đã được tiếp nhận</p>
                <p className="mt-1 text-emerald-700 leading-relaxed">{result.message}</p>
              </div>
            </div>

            {result.previewLink && (
              <div className="p-3 bg-alabaster-50 rounded border border-emerald-200 text-stone-700 space-y-1">
                <span className="font-semibold text-[11px] text-stone-500 uppercase block">
                  Liên kết mô phỏng (Demo Token Preview):
                </span>
                <code className="text-[11px] font-mono text-terracotta-700 break-all block">
                  {result.previewLink}
                </code>
              </div>
            )}

            <div className="pt-2">
              <Link
                href="/login"
                className="inline-flex items-center gap-1.5 px-4 py-2 rounded-atelier bg-emerald-700 text-alabaster-50 font-medium text-xs hover:bg-emerald-800 transition-colors"
              >
                <ArrowLeft className="h-3.5 w-3.5" />
                <span>Quay lại trang Đăng nhập</span>
              </Link>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Disclaimer pill */}
            <div className="p-3 bg-ochre-50/80 rounded-atelier border border-ochre-200 text-xs text-ochre-800 flex items-start gap-2">
              <Sparkles className="h-4 w-4 text-ochre-600 shrink-0 mt-0.5" />
              <span>
                <strong>Frontend Demo Mode:</strong> Chức năng chạy mô phỏng giao diện, không phát sinh email hay SMS thật.
              </span>
            </div>

            {error && (
              <div className="p-3 rounded-atelier bg-red-50 border border-red-200 text-xs text-red-700 flex items-start gap-2">
                <AlertCircle className="h-4 w-4 text-red-600 shrink-0 mt-0.5" />
                <span>{error}</span>
              </div>
            )}

            <div>
              <label className="block text-xs font-medium text-stone-700 mb-1">
                Địa chỉ Email tài khoản <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <Mail className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                <input
                  type="email"
                  required
                  placeholder="name@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full pl-9 pr-3 py-2.5 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 px-4 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 font-medium text-xs transition-colors shadow-sm flex items-center justify-center gap-2 mt-4"
            >
              {loading ? (
                <div className="h-4 w-4 rounded-full border-2 border-alabaster-50 border-t-transparent animate-spin" />
              ) : (
                <>
                  <span>Gửi hướng dẫn khôi phục mật khẩu</span>
                  <ArrowRight className="h-4 w-4" />
                </>
              )}
            </button>
          </form>
        )}

        {/* Back to login */}
        <div className="pt-4 border-t border-stone-200/80 text-center text-xs text-stone-600">
          <Link
            href="/login"
            className="inline-flex items-center gap-1.5 font-medium text-stone-600 hover:text-espresso-900 transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            <span>Quay lại trang Đăng nhập</span>
          </Link>
        </div>
      </div>
    </PortalAuthLayout>
  );
}

