import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Aspire Single Page",
  description: "A boilerplate single page experience for the Aspire webapp",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
