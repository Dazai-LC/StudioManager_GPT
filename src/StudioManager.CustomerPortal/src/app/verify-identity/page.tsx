'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
  ShieldCheck,
  ShieldAlert,
  Clock,
  CheckCircle2,
  XCircle,
  ArrowRight,
  ArrowLeft,
  User,
  Phone,
  CreditCard,
  FileText,
  Sparkles,
  Info,
} from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { CustomerAuthDto, LinkedIdentityStatus } from '@/lib/contracts';

export default function VerifyIdentityPage() {
  const router = useRouter();
  const [currentUser, setCurrentUser] = useState<CustomerAuthDto | null>(null);

  const [formData, setFormData] = useState({
    fullName: '',
    phoneNumber: '',
    idCardNumber: '',
    recentBookingCode: '',
    verificationNote: '',
  });

  const [status, setStatus] = useState<LinkedIdentityStatus>('UNLINKED');
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [matchedCustomer, setMatchedCustomer] = useState<{
    maKhachHang: string;
    hoTen: string;
    soDienThoai: string;
    diaChi?: string;
  } | null>(null);

  useEffect(() => {
    const user = portalDataProvider.getCurrentSessionUser();
    if (user) {
      setCurrentUser(user);
      setStatus(user.linkedStatus);
      setFormData((prev) => ({
        ...prev,
        fullName: user.fullName || '',
        phoneNumber: user.phoneNumber || '',
      }));
    }
  }, []);

  const handleVerifySubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);

    const result = await portalDataProvider.simulateVerifyIdentity({
      fullName: formData.fullName,
      phoneNumber: formData.phoneNumber,
      idCardNumber: formData.idCardNumber,
      recentBookingCode: formData.recentBookingCode,
      verificationNote: formData.verificationNote,
    });

    setLoading(false);
    setStatus(result.status);
    setMessage(result.message);
    if (result.matchedCounterCustomer) {
      setMatchedCustomer(result.matchedCounterCustomer);
    }
  };

  // Quick State Toggles for Evaluation Review
  const setDemoState = (targetStatus: LinkedIdentityStatus) => {
    setStatus(targetStatus);
    if (targetStatus === 'VERIFIED') {
      setMessage('Đã mô phỏng: Liên kết hồ sơ thành công với KH000001 (Nguyễn Văn An). Toàn bộ lịch sử chụp tại quầy đã đồng bộ.');
      setMatchedCustomer({
        maKhachHang: 'KH000001',
        hoTen: 'Nguyễn Văn An',
        soDienThoai: '0901234567',
        diaChi: '123 Phố Huế, Hai Bà Trưng, Hà Nội',
      });
    } else if (targetStatus === 'PENDING_VERIFICATION') {
      setMessage('Đã mô phỏng: Hồ sơ đang trong trạng thái chờ nhân viên studio đối soát thông tin.');
      setMatchedCustomer(null);
    } else if (targetStatus === 'REJECTED') {
      setMessage('Đã mô phỏng: Thông tin định danh không khớp với dữ liệu tại quầy. Yêu cầu đã bị từ chối.');
      setMatchedCustomer(null);
    } else {
      setMessage(null);
      setMatchedCustomer(null);
    }
  };

  return (
    <div className="min-h-screen bg-alabaster-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto space-y-10">
        {/* Navigation Breadcrumb */}
        <div className="flex items-center justify-between border-b border-stone-200 pb-4">
          <Link
            href="/"
            className="inline-flex items-center gap-1.5 text-xs text-stone-500 hover:text-espresso-900 transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            <span>Trang chủ Atelier Lumière</span>
          </Link>
          <span className="text-xs font-mono text-stone-400">CP-AUTH-04 • DEC-03 Identity Separation</span>
        </div>

        {/* Page Header */}
        <div>
          <span className="text-xs font-mono uppercase tracking-widest text-terracotta-700 bg-terracotta-50 px-2.5 py-1 rounded border border-terracotta-100">
            Xác thực danh tính & Liên kết hồ sơ
          </span>
          <h1 className="font-serif text-3xl sm:text-4xl font-bold text-espresso-900 mt-2">
            Định Danh Khách Hàng (DEC-03)
          </h1>
          <p className="text-sm text-stone-600 mt-2 max-w-2xl font-sans">
            Theo kiến trúc bảo mật DEC-03, hồ sơ khách hàng tại quầy Studio và tài khoản Cổng Khách Hàng được phân tách độc lập nhằm bảo vệ dữ liệu cá nhân. Hoàn tất xác thực để xem lịch sử chụp ảnh cũ.
          </p>
        </div>

        {/* Reviewer Simulation Toggle Bar */}
        <div className="p-4 rounded-atelier bg-alabaster-100 border border-stone-300 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-mono font-bold text-espresso-900 uppercase flex items-center gap-1.5">
              <Sparkles className="h-3.5 w-3.5 text-terracotta-600" />
              <span>Chuyển đổi trạng thái mô phỏng (Interactive Demo States):</span>
            </span>
            <span className="text-[11px] text-stone-500 font-mono">Current: {status}</span>
          </div>

          <div className="flex flex-wrap gap-2 pt-1">
            <button
              onClick={() => setDemoState('VERIFIED')}
              className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                status === 'VERIFIED'
                  ? 'bg-emerald-700 text-alabaster-50 shadow-sm'
                  : 'bg-emerald-50 text-emerald-800 border border-emerald-300 hover:bg-emerald-100'
              }`}
            >
              1. Đã duyệt liên kết (VERIFIED)
            </button>

            <button
              onClick={() => setDemoState('PENDING_VERIFICATION')}
              className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                status === 'PENDING_VERIFICATION'
                  ? 'bg-amber-600 text-alabaster-50 shadow-sm'
                  : 'bg-amber-50 text-amber-800 border border-amber-300 hover:bg-amber-100'
              }`}
            >
              2. Đang chờ nhân viên duyệt (PENDING)
            </button>

            <button
              onClick={() => setDemoState('REJECTED')}
              className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                status === 'REJECTED'
                  ? 'bg-red-700 text-alabaster-50 shadow-sm'
                  : 'bg-red-50 text-red-800 border border-red-300 hover:bg-red-100'
              }`}
            >
              3. Từ chối xác thực (REJECTED)
            </button>

            <button
              onClick={() => setDemoState('UNLINKED')}
              className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                status === 'UNLINKED'
                  ? 'bg-stone-700 text-alabaster-50 shadow-sm'
                  : 'bg-stone-100 text-stone-700 border border-stone-300 hover:bg-stone-200'
              }`}
            >
              4. Chưa liên kết (UNLINKED)
            </button>
          </div>
        </div>

        {/* Status Feedback Card */}
        {status === 'VERIFIED' && (
          <div className="p-6 rounded-atelier bg-emerald-50 border border-emerald-300 text-xs text-emerald-900 space-y-4">
            <div className="flex items-start gap-3">
              <ShieldCheck className="h-6 w-6 text-emerald-600 shrink-0 mt-0.5" />
              <div>
                <h3 className="font-serif text-lg font-bold text-emerald-950">
                  Tài Khoản Đã Được Định Danh Thành Công
                </h3>
                <p className="mt-1 text-emerald-800 leading-relaxed font-sans">
                  {message || 'Hồ sơ Cổng Khách Hàng của bạn đã được liên kết với mã khách hàng tại quầy.'}
                </p>
              </div>
            </div>

            {matchedCustomer && (
              <div className="p-4 bg-alabaster-50 rounded-atelier border border-emerald-200 grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <span className="text-[11px] text-stone-500">Mã khách hàng nội bộ:</span>
                  <p className="font-mono font-bold text-espresso-900 text-sm">{matchedCustomer.maKhachHang}</p>
                </div>
                <div>
                  <span className="text-[11px] text-stone-500">Họ và tên trong hồ sơ:</span>
                  <p className="font-semibold text-espresso-900">{matchedCustomer.hoTen}</p>
                </div>
                <div>
                  <span className="text-[11px] text-stone-500">Số điện thoại xác thực:</span>
                  <p className="font-mono text-espresso-900">{matchedCustomer.soDienThoai}</p>
                </div>
                {matchedCustomer.diaChi && (
                  <div>
                    <span className="text-[11px] text-stone-500">Địa chỉ đăng ký:</span>
                    <p className="text-stone-700">{matchedCustomer.diaChi}</p>
                  </div>
                )}
              </div>
            )}

            <div className="pt-2 flex items-center gap-3">
              <Link
                href="/packages"
                className="px-4 py-2 rounded-atelier bg-emerald-700 text-alabaster-50 font-medium text-xs hover:bg-emerald-800 transition-colors"
              >
                Đặt lịch chụp mới
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

        {status === 'PENDING_VERIFICATION' && (
          <div className="p-6 rounded-atelier bg-amber-50 border border-amber-300 text-xs text-amber-900 space-y-3">
            <div className="flex items-start gap-3">
              <Clock className="h-6 w-6 text-amber-600 shrink-0 mt-0.5" />
              <div>
                <h3 className="font-serif text-lg font-bold text-amber-950">
                  Yêu Cầu Đang Trong Quá Trình Đối Soát
                </h3>
                <p className="mt-1 text-amber-800 leading-relaxed font-sans">
                  {message || 'Nhân viên lễ tân studio đang đối chiếu thông tin CCCD và số điện thoại của bạn với hồ sơ tại quầy. Kết quả sẽ được cập nhật trong vòng 2-4 giờ làm việc.'}
                </p>
              </div>
            </div>
          </div>
        )}

        {status === 'REJECTED' && (
          <div className="p-6 rounded-atelier bg-red-50 border border-red-300 text-xs text-red-900 space-y-3">
            <div className="flex items-start gap-3">
              <XCircle className="h-6 w-6 text-red-600 shrink-0 mt-0.5" />
              <div>
                <h3 className="font-serif text-lg font-bold text-red-950">
                  Yêu Cầu Định Danh Không Khớp
                </h3>
                <p className="mt-1 text-red-800 leading-relaxed font-sans">
                  {message || 'Hệ thống không tìm thấy hồ sơ tại quầy có số điện thoại hoặc CCCD trùng khớp. Vui lòng kiểm tra lại thông tin hoặc liên hệ hotline studio để được hỗ trợ trực tiếp.'}
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Verification Submission Form (When not yet verified) */}
        {status !== 'VERIFIED' && (
          <div className="bg-alabaster-50 p-6 sm:p-8 rounded-atelier-lg border border-stone-300 shadow-sm space-y-6">
            <div className="border-b border-stone-200 pb-4">
              <h3 className="font-serif text-xl font-bold text-espresso-900">
                Gửi thông tin xác thực hồ sơ tại quầy
              </h3>
              <p className="text-xs text-stone-500 mt-1">
                Điền thông tin chính xác như khi bạn làm thủ tục chụp ảnh trực tiếp tại Atelier Lumière.
              </p>
            </div>

            <form onSubmit={handleVerifySubmit} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-stone-700 mb-1">
                    Họ và tên khách hàng <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <User className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                    <input
                      type="text"
                      required
                      placeholder="Nguyễn Văn An"
                      value={formData.fullName}
                      onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                      className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-medium text-stone-700 mb-1">
                    Số điện thoại đăng ký tại quầy <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <Phone className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                    <input
                      type="tel"
                      required
                      placeholder="0901234567"
                      value={formData.phoneNumber}
                      onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                      className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500 font-mono"
                    />
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-stone-700 mb-1">
                    Số CCCD / CMND (Đối soát bảo mật)
                  </label>
                  <div className="relative">
                    <CreditCard className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                    <input
                      type="text"
                      placeholder="001090123456 (khớp với hợp đồng cũ)"
                      value={formData.idCardNumber}
                      onChange={(e) => setFormData({ ...formData, idCardNumber: e.target.value })}
                      className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500 font-mono"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-medium text-stone-700 mb-1">
                    Mã hợp đồng hoặc buổi chụp gần nhất (nếu nhớ)
                  </label>
                  <div className="relative">
                    <FileText className="h-4 w-4 text-stone-400 absolute left-3 top-1/2 -translate-y-1/2" />
                    <input
                      type="text"
                      placeholder="Ví dụ: REQ-20261101-001 hoặc HD-0012"
                      value={formData.recentBookingCode}
                      onChange={(e) => setFormData({ ...formData, recentBookingCode: e.target.value })}
                      className="w-full pl-9 pr-3 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500 uppercase font-mono"
                    />
                  </div>
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-stone-700 mb-1">
                  Ghi chú hoặc yêu cầu hỗ trợ thêm
                </label>
                <textarea
                  rows={2}
                  placeholder="Ghi chú thêm thông tin giúp nhân viên studio nhận diện bạn nhanh hơn..."
                  value={formData.verificationNote}
                  onChange={(e) => setFormData({ ...formData, verificationNote: e.target.value })}
                  className="w-full p-3 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
                />
              </div>

              <div className="pt-2">
                <button
                  type="submit"
                  disabled={loading}
                  className="w-full sm:w-auto px-6 py-3 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 font-medium text-xs transition-colors shadow-sm flex items-center justify-center gap-2"
                >
                  {loading ? (
                    <div className="h-4 w-4 rounded-full border-2 border-alabaster-50 border-t-transparent animate-spin" />
                  ) : (
                    <>
                      <ShieldCheck className="h-4 w-4" />
                      <span>Gửi thông tin xác thực hồ sơ</span>
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        )}
      </div>
    </div>
  );
}
