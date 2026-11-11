'use client';

import React from 'react';
import { X, Calendar, Clock, Sparkles, CheckCircle2, ArrowRight } from 'lucide-react';
import Link from 'next/link';

interface BookingPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedPackageTitle?: string;
  selectedPackageCode?: string;
}

export const BookingPreviewModal: React.FC<BookingPreviewModalProps> = ({
  isOpen,
  onClose,
  selectedPackageTitle,
  selectedPackageCode,
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-espresso-900/60 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="relative w-full max-w-lg rounded-atelier bg-alabaster-50 border border-stone-300 shadow-2xl p-6 overflow-hidden">
        {/* Subtle accent border at top */}
        <div className="absolute top-0 inset-x-0 h-1 bg-gradient-to-r from-terracotta-600 via-ochre-600 to-terracotta-500" />

        {/* Close Button */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-1.5 rounded-full text-stone-500 hover:text-espresso-900 hover:bg-stone-200/60 transition-colors"
          aria-label="Đóng"
        >
          <X className="h-5 w-5" />
        </button>

        {/* Header */}
        <div className="flex items-center gap-3 mb-4">
          <div className="p-2.5 rounded-full bg-terracotta-50 text-terracotta-600 border border-terracotta-100">
            <Sparkles className="h-5 w-5" />
          </div>
          <div>
            <span className="text-[11px] font-mono uppercase tracking-wider text-terracotta-700 bg-terracotta-50 px-2 py-0.5 rounded border border-terracotta-100">
              Phase 03 Booking Workflow Preview
            </span>
            <h3 className="font-serif text-xl font-semibold text-espresso-900 mt-1">
              Trải nghiệm Đặt lịch Trực tuyến
            </h3>
          </div>
        </div>

        {/* Body content */}
        <div className="space-y-4 text-sm text-stone-600">
          {selectedPackageTitle && (
            <div className="p-3.5 rounded-atelier bg-alabaster-100 border border-stone-200 flex items-center justify-between">
              <div>
                <span className="text-xs text-stone-500">Gói chụp đang chọn:</span>
                <p className="font-medium text-espresso-900">{selectedPackageTitle}</p>
              </div>
              {selectedPackageCode && (
                <span className="text-xs font-mono px-2 py-1 bg-alabaster-50 rounded border border-stone-300 text-espresso-800">
                  {selectedPackageCode}
                </span>
              )}
            </div>
          )}

          <p className="leading-relaxed">
            Bạn đang trải nghiệm bản thử nghiệm giao diện <strong>Phase 02 (Customer Portal Shell & Public Discovery)</strong> của Atelier Lumière.
          </p>

          <div className="p-4 rounded-atelier bg-ochre-50/70 border border-ochre-200 text-xs text-ochre-800 space-y-2">
            <p className="font-semibold text-ochre-900 flex items-center gap-1.5">
              <Calendar className="h-4 w-4 text-ochre-700" />
              Quy trình đặt lịch đầy đủ sẽ ra mắt tại Phase 03:
            </p>
            <ul className="space-y-1.5 pl-1">
              <li className="flex items-start gap-2">
                <CheckCircle2 className="h-3.5 w-3.5 text-ochre-700 shrink-0 mt-0.5" />
                <span>Chọn ngày và khung giờ chụp theo thời gian thực (Slot Picker)</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle2 className="h-3.5 w-3.5 text-ochre-700 shrink-0 mt-0.5" />
                <span>Lựa chọn phòng chụp (Phòng Ánh Sáng, Cổ Điển, Em Bé)</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle2 className="h-3.5 w-3.5 text-ochre-700 shrink-0 mt-0.5" />
                <span>Tuỳ chọn dịch vụ gia tăng (Trang điểm, thuê trang phục, in album)</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle2 className="h-3.5 w-3.5 text-ochre-700 shrink-0 mt-0.5" />
                <span>Thanh toán đặt cọc chuẩn VietQR & đối soát nhân viên minh bạch</span>
              </li>
            </ul>
          </div>

          <p className="text-xs text-stone-500 italic">
            * Hiện tại, bạn có thể tham quan các không gian chụp, khám phá chi tiết 5 gói dịch vụ seed hoặc trải nghiệm đăng ký tài khoản portal với cơ chế phân tách danh tính DEC-03.
          </p>
        </div>

        {/* Footer Actions */}
        <div className="mt-6 flex flex-col sm:flex-row items-center justify-end gap-2.5 pt-4 border-t border-stone-200">
          <button
            onClick={onClose}
            className="w-full sm:w-auto px-4 py-2 rounded-atelier text-stone-600 hover:text-espresso-900 hover:bg-stone-100 text-sm font-medium transition-colors"
          >
            Đóng cửa sổ
          </button>
          <Link
            href="/packages"
            onClick={onClose}
            className="w-full sm:w-auto px-4 py-2 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 text-sm font-medium transition-colors flex items-center justify-center gap-1.5 shadow-sm"
          >
            <span>Khám phá các gói chụp</span>
            <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
      </div>
    </div>
  );
};

