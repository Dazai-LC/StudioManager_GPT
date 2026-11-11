'use client';

import React from 'react';
import Link from 'next/link';
import { Camera, Heart, Sparkles, ShieldCheck, ArrowRight, ArrowLeft } from 'lucide-react';
import { VisualPlaceholder } from '@/components/VisualPlaceholder';

export default function AboutPage() {
  return (
    <div className="min-h-screen bg-alabaster-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-5xl mx-auto space-y-16">
        {/* Navigation Breadcrumb */}
        <div>
          <Link
            href="/"
            className="inline-flex items-center gap-1.5 text-xs text-stone-500 hover:text-espresso-900 transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            <span>Trang chủ Atelier Lumière</span>
          </Link>
        </div>

        {/* Hero Editorial */}
        <div className="text-center space-y-4 max-w-3xl mx-auto">
          <span className="text-xs font-mono uppercase tracking-widest text-terracotta-700 bg-terracotta-50 px-3 py-1 rounded-full border border-terracotta-100">
            Triết lý nghệ thuật & Di sản thương hiệu
          </span>
          <h1 className="font-serif text-4xl sm:text-5xl font-bold text-espresso-900 leading-tight">
            Nơi ánh sáng tôn vinh <br />
            những khoảnh khắc chân thực nhất
          </h1>
          <p className="text-base sm:text-lg text-stone-600 font-light leading-relaxed font-sans">
            Thành lập với sứ mệnh định hình lại trải nghiệm nhiếp ảnh studio cao cấp tại Việt Nam, Atelier Lumière là sự giao thoa hài hòa giữa thẩm mỹ Warm Editorial cổ điển và công nghệ xử lý hình ảnh hiện đại.
          </p>
        </div>

        {/* Split Manifesto */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-10 items-center">
          <div className="space-y-6 text-stone-700 text-sm leading-relaxed">
            <h3 className="font-serif text-2xl font-bold text-espresso-900">
              Không gian sáng tạo không giới hạn
            </h3>
            <p>
              Tại Atelier Lumière, mỗi buổi chụp ảnh không đơn thuần là quá trình bấm máy. Đó là một buổi đối thoại cảm xúc giữa người chụp và nghệ sĩ nhiếp ảnh. Chúng tôi lắng nghe phong cách, mong muốn và câu chuyện riêng biệt của từng khách hàng trước khi điều chỉnh từng nguồn sáng.
            </p>
            <p>
              Hệ thống phòng chụp của chúng tôi được thiết kế theo tiêu chuẩn âm thanh và ánh sáng khắt khe, tách biệt hoàn toàn với nhịp sống ồn ào bên ngoài, tạo nên không gian tĩnh lặng để bạn tự do bộc lộ thần thái tự nhiên nhất.
            </p>

            <div className="pt-2 grid grid-cols-2 gap-4 text-xs">
              <div className="p-3 bg-alabaster-100 rounded-atelier border border-stone-200">
                <span className="font-serif text-xl font-bold text-terracotta-700 block">03</span>
                <span className="text-stone-600">Phòng chụp chuyên biệt</span>
              </div>
              <div className="p-3 bg-alabaster-100 rounded-atelier border border-stone-200">
                <span className="font-serif text-xl font-bold text-terracotta-700 block">4K</span>
                <span className="text-stone-600">Chuẩn bàn giao hình ảnh</span>
              </div>
            </div>
          </div>

          <div>
            <VisualPlaceholder
              assetId="BG-AUTH-01"
              title="Atelier Lumière Heritage"
              subtitle="Không gian nghệ thuật & Phong cách Warm Editorial"
              category="auth-bg"
              aspectRatio="square"
              className="shadow-xl"
            />
          </div>
        </div>

        {/* Values Section */}
        <div className="bg-alabaster-100 p-8 sm:p-12 rounded-atelier-lg border border-stone-200/80">
          <h3 className="font-serif text-2xl font-bold text-espresso-900 text-center mb-10">
            3 Giá trị cốt lõi tạo nên sự khác biệt
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="space-y-3">
              <div className="h-10 w-10 rounded-full bg-terracotta-50 text-terracotta-600 border border-terracotta-100 flex items-center justify-center">
                <Sparkles className="h-5 w-5" />
              </div>
              <h4 className="font-serif text-lg font-bold text-espresso-900">
                Ánh sáng điện ảnh
              </h4>
              <p className="text-xs text-stone-600 leading-relaxed">
                Ứng dụng kỹ thuật chiếu sáng tương phản cao và ánh sáng dịu tự nhiên, tạo chiều sâu thị giác độc đáo mà các bộ lọc kỹ thuật số không thể thay thế.
              </p>
            </div>

            <div className="space-y-3">
              <div className="h-10 w-10 rounded-full bg-terracotta-50 text-terracotta-600 border border-terracotta-100 flex items-center justify-center">
                <Heart className="h-5 w-5" />
              </div>
              <h4 className="font-serif text-lg font-bold text-espresso-900">
                Cảm xúc chân thật
              </h4>
              <p className="text-xs text-stone-600 leading-relaxed">
                Tôn trọng nét đẹp tự nhiên của từng khuôn mặt và dáng vóc. Hậu kỳ màu sắc và làn da mềm mại, tránh việc chỉnh sửa quá đà làm mất bản sắc cá nhân.
              </p>
            </div>

            <div className="space-y-3">
              <div className="h-10 w-10 rounded-full bg-terracotta-50 text-terracotta-600 border border-terracotta-100 flex items-center justify-center">
                <ShieldCheck className="h-5 w-5" />
              </div>
              <h4 className="font-serif text-lg font-bold text-espresso-900">
                Lưu trữ vĩnh viễn
              </h4>
              <p className="text-xs text-stone-600 leading-relaxed">
                Toàn bộ dữ liệu ảnh gốc và ảnh chỉnh sửa được quản lý trên nền tảng StudioManager hiện đại, bảo đảm an toàn dữ liệu và khả năng tải về trọn đời.
              </p>
            </div>
          </div>
        </div>

        {/* CTA Banner */}
        <div className="text-center pt-8">
          <Link
            href="/packages"
            className="inline-flex items-center gap-2 px-8 py-3.5 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 font-medium text-sm shadow-md transition-all"
          >
            <span>Khám phá 5 gói chụp tại Studio</span>
            <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
      </div>
    </div>
  );
}

