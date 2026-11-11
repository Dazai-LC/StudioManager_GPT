'use client';

import React from 'react';
import { Check, X } from 'lucide-react';

interface PasswordStrengthMeterProps {
  password: string;
}

export const PasswordStrengthMeter: React.FC<PasswordStrengthMeterProps> = ({ password }) => {
  if (!password) return null;

  const hasLength = password.length >= 8;
  const hasUpperCase = /[A-Z]/.test(password);
  const hasNumber = /[0-9]/.test(password);
  const hasSpecial = /[^A-Za-z0-9]/.test(password);

  const score = [hasLength, hasUpperCase, hasNumber, hasSpecial].filter(Boolean).length;

  const strengthConfig = [
    { label: 'Rất yếu', color: 'bg-red-500', textColor: 'text-red-600', percent: 20 },
    { label: 'Yếu', color: 'bg-orange-500', textColor: 'text-orange-600', percent: 40 },
    { label: 'Trung bình', color: 'bg-yellow-500', textColor: 'text-yellow-700', percent: 65 },
    { label: 'Tốt', color: 'bg-emerald-500', textColor: 'text-emerald-700', percent: 85 },
    { label: 'Rất mạnh', color: 'bg-emerald-600', textColor: 'text-emerald-800', percent: 100 },
  ];

  const currentStrength = strengthConfig[score];

  return (
    <div className="mt-2 space-y-2">
      <div className="flex items-center justify-between text-xs">
        <span className="text-stone-500">Độ an toàn mật khẩu:</span>
        <span className={`font-medium ${currentStrength.textColor}`}>{currentStrength.label}</span>
      </div>

      <div className="h-1.5 w-full bg-stone-200 rounded-full overflow-hidden">
        <div
          className={`h-full transition-all duration-300 ${currentStrength.color}`}
          style={{ width: `${currentStrength.percent}%` }}
        />
      </div>

      <div className="grid grid-cols-2 gap-1 text-[11px] text-stone-500 pt-1">
        <span className={`flex items-center gap-1 ${hasLength ? 'text-emerald-600' : 'text-stone-400'}`}>
          {hasLength ? <Check className="h-3 w-3" /> : <X className="h-3 w-3" />}
          Tối thiểu 8 ký tự
        </span>
        <span className={`flex items-center gap-1 ${hasUpperCase ? 'text-emerald-600' : 'text-stone-400'}`}>
          {hasUpperCase ? <Check className="h-3 w-3" /> : <X className="h-3 w-3" />}
          Chữ in hoa
        </span>
        <span className={`flex items-center gap-1 ${hasNumber ? 'text-emerald-600' : 'text-stone-400'}`}>
          {hasNumber ? <Check className="h-3 w-3" /> : <X className="h-3 w-3" />}
          Chữ số (0-9)
        </span>
        <span className={`flex items-center gap-1 ${hasSpecial ? 'text-emerald-600' : 'text-stone-400'}`}>
          {hasSpecial ? <Check className="h-3 w-3" /> : <X className="h-3 w-3" />}
          Ký tự đặc biệt (@, #, $)
        </span>
      </div>
    </div>
  );
};

