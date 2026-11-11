import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        alabaster: {
          50: "#FAF8F5",
          100: "#F4F1EA",
          200: "#E9E4DA",
        },
        stone: {
          300: "#D6D0C4",
          400: "#A8A094",
          500: "#7C7469",
        },
        umber: {
          700: "#4A443D",
        },
        espresso: {
          800: "#24211E",
          900: "#161412",
        },
        terracotta: {
          50: "#FDF4F0",
          100: "#FAE1D6",
          200: "#F3C1AC",
          500: "#D16038",
          600: "#C04E26",
          700: "#A13D1B",
          800: "#822F14",
        },
        sage: {
          50: "#EEF7F2",
          200: "#B7E0CA",
          600: "#2B7250",
          700: "#1F593D",
        },
        ochre: {
          50: "#FEF9EB",
          200: "#F9E0A2",
          600: "#B86E0E",
          700: "#945608",
        },
        crimson: {
          50: "#FEF2F2",
          200: "#FCA2A2",
          600: "#C42B2B",
        },
        teal: {
          50: "#EEF8F9",
          200: "#B0E0E6",
          600: "#167280",
        },
        brand: {
          primary: "#C04E26",
          hover: "#A13D1B",
          light: "#FDF4F0",
          dark: "#161412",
          bg: "#F4F1EA",
          card: "#FAF8F5",
          border: "#E9E4DA",
          text: "#161412",
          muted: "#7C7469",
        },
      },
      fontFamily: {
        serif: ["Georgia", "Cambria", "Times New Roman", "serif"],
        sans: ["Segoe UI", "-apple-system", "BlinkMacSystemFont", "Roboto", "sans-serif"],
        mono: ["Consolas", "Courier New", "monospace"],
      },
      borderRadius: {
        atelier: "8px",
        "atelier-lg": "12px",
      },
    },
  },
  plugins: [],
};
export default config;

