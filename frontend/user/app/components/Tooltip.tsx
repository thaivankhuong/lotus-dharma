'use client';

import React from 'react';
import { TooltipProps } from '../types/map';

export default function Tooltip({ visible, x, y, content }: TooltipProps) {
    if (!visible || !content) return null;

    return (
        <div
            className="fixed pointer-events-none z-50 transition-opacity duration-200"
            style={{
                left: `${x + 15}px`,
                top: `${y + 15}px`,
                opacity: visible ? 1 : 0,
            }}
        >
            <div className="bg-white px-3 py-2 rounded-lg shadow-lg border border-gray-200">
                <p className="text-sm font-medium text-gray-800 whitespace-nowrap">
                    {content}
                </p>
            </div>
        </div>
    );
}
