/** @type {import('tailwindcss').Config} */
module.exports = {
  daisyui: {
    themes: [
      {
        night: {
          ...require("daisyui/src/theming/themes")["[data-theme=night]"],
          "primary": "#104270",
          "secondary": "#8c0053",
          "accent": "#7edcb9",
          "neutral": "#2a323c",
          "base-100": "#1d232a",
          "info": "#3abff8",
          "success": "#36d399",
          "warning": "#fbbd23",
          "error": "#f87272",
        },
        light: {
          ...require("daisyui/src/theming/themes")["[data-theme=light]"],
          "primary": "#104270",
          "secondary": "#8c0053",
          "accent": "#7edcb9",
        },
      },
    ],
  },
  content: ["./DHConsulting/Views/**/*.cshtml"],
  theme: {
    extend: {},
  },
  plugins: [require("daisyui")],
}

