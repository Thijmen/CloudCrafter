import '@repo/ui/main.css'
import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import { CloudCraftProvider } from './providers/CloudCraftProvider'

const inter = Inter({ subsets: ['latin'] })

export const metadata: Metadata = {
    title: 'Docs',
    description: 'Generated by create turbo',
}

export default function RootLayout({
                                       children,
                                   }: {
    children: React.ReactNode;
}): JSX.Element {
    return (
        <html lang="en">
        <body className={inter.className}>
        <CloudCraftProvider>{children}</CloudCraftProvider></body>
        </html>
    )
}