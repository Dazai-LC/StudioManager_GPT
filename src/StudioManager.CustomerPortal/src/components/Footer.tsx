'use client';

import React from 'react';
import Link from 'next/link';
import { Camera, MapPin, Phone, Mail, Clock, ShieldCheck } from 'lucide-react';

export const Footer: React.FC = () => {
  return (
    <footer className="bg-espresso-900 text-alabaster-50 pt-16 pb-24 md:pb-12 border-t border-stone-800">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-10 pb-12 border-b border-stone-800">
          {/* Brand Story */}
          <div className="space-y-4">
            <div className="flex items-center gap-3">
              <div className="h-9 w-9 rounded-atelier bg-alabaster-50 text-espresso-900 flex items-center justify-center">
                <Camera className="h-5 w-5 text-terracotta-600" />
              </div>
              <span className="font-serif text-xl font-bold tracking-tight text-alabaster-50">
                ATELIER LUMIÈRE
              </span>
            </div>
            <p className="text-xs text-stone-300 font-light leading-relaxed">
              Studio nhiếp ảnh nghệ thuật cao cấp. Nơi lưu giữ những xúc cảm chân thực nhất qua lăng kính điện ảnh và phong cách Warm Editorial đương đại.
            </p>
            <div className="pt-2 text-xs text-stone-400 space-y-1">
              <p className="font-mono text-[11px] text-terracotta-400">STUDIOMANAGER v2.2 ECOSYSTEM</p>
              <p>Mã dự án: Project Phoenix</p>
            </div>
          </div>

          {/* Contact Information */}
          <div className="space-y-3">
            <h4 className="font-serif text-base font-semibold text-alabaster-50">
              Thông tin liên hệ
            </h4>
            <ul className="space-y-2.5 text-xs text-stone-300">
              <li className="flex items-start gap-2.5">
                <MapPin className="h-4 w-4 text-terracotta-500 shrink-0 mt-0.5" />
                <span>128 Phố Huế, Quận Hai Bà Trưng, Hà Nội</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Phone className="h-4 w-4 text-terracotta-500 shrink-0" />
                <span>0903 123 456 / 024 3822 9999</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Mail className="h-4 w-4 text-terracotta-500 shrink-0" />
                <span>contact@atelierlumiere.vn</span>
              </li>
              <li className="flex items-start gap-2.5">
                <Clock className="h-4 w-4 text-terracotta-500 shrink-0 mt-0.5" />
                <span>Thứ 2 – Chủ Nhật: 08:30 – 21:00</span>
              </li>
            </ul>
          </div>

          {/* Quick Links */}
          <div className="space-y-3">
            <h4 className="font-serif text-base font-semibold text-alabaster-50">
              Khám phá Studio
            </h4>
            <ul className="space-y-2 text-xs text-stone-300">
              <li>
                <Link href="/packages" className="hover:text-terracotta-400 transition-colors">
                  Bộ sưu tập 5 Gói chụp (GC000001 – 05)
                </Link>
              </li>
              <li>
                <Link href="/studio-showcase" className="hover:text-terracotta-400 transition-colors">
                  Phòng Ánh Sáng (P0001)
                </Link>
              </li>
              <li>
                <Link href="/studio-showcase" className="hover:text-terracotta-400 transition-colors">
                  Phòng Cổ Điển (P0002)
                </Link>
              </li>
              <li>
                <Link href="/studio-showcase" className="hover:text-terracotta-400 transition-colors">
                  Phòng Em Bé (P0003)
                </Link>
              </li>
              <li>
                <Link href="/about" className="hover:text-terracotta-400 transition-colors">
                  Triết lý nhiếp ảnh & Đội ngũ
                </Link>
              </li>
            </ul>
          </div>

          {/* Customer Portal & Security Policy */}
          <div className="space-y-3">
            <h4 className="font-serif text-base font-semibold text-alabaster-50">
              Cổng Khách Hàng
            </h4>
            <ul className="space-y-2 text-xs text-stone-300">
              <li>
                <Link href="/login" className="hover:text-terracotta-400 transition-colors">
                  Đăng nhập tài khoản portal
                </Link>
              </li>
              <li>
                <Link href="/register" className="hover:text-terracotta-400 transition-colors">
                  Đăng ký thành viên mới
                </Link>
              </li>
              <li>
                <Link href="/recovery" className="hover:text-terracotta-400 transition-colors">
                  Khôi phục mật khẩu
                </Link>
              </li>
              <li>
                <Link href="/verify-identity" className="hover:text-terracotta-400 transition-colors">
                  Xác thực & Liên kết hồ sơ tại quầy (DEC-03)
                </Link>
              </li>
            </ul>
            <div className="pt-2 flex items-center gap-1.5 text-[11px] text-emerald-400">
              <ShieldCheck className="h-3.5 w-3.5" />
              <span>Chính sách bảo mật danh tính DEC-03</span>
            </div>
          </div>
        </div>

        {/* Bottom copyright */}
        <div className="pt-8 flex flex-col md:flex-row items-center justify-between text-xs text-stone-400 gap-4">
          <p>© 2026 Atelier Lumière. All rights reserved. StudioManager v2.2 Customer Portal.</p>
          <div className="flex items-center gap-6 text-[11px]">
            <span>Chế độ Demo UI không lưu cơ sở dữ liệu thật</span>
            <span>VietQR & Đối soát thanh toán chuẩn mực</span>
          </div>
        </div>
      </div>
    </footer>
  );
};

