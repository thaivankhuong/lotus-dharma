'use client';

import { useEffect, useState } from 'react';
import dynamic from 'next/dynamic';
import type { VietnamMapData } from '../types/map';
import { loadVietnamMapData } from '../utils/vietnamDataLoader';
import { MergerUnit } from '../types/merger';

// Dynamically import Leaflet to avoid SSR issues
const MapComponent = dynamic(() => import('./VietnamMapClient'), {
    ssr: false,
    loading: () => (
        <div className="flex items-center justify-center h-screen bg-gradient-to-br from-slate-900 to-slate-800">
            <div className="text-center">
                <div className="animate-spin rounded-full h-16 w-16 border-t-4 border-b-4 border-blue-500 mx-auto mb-4"></div>
                <p className="text-white text-lg">Đang khởi tạo bản đồ...</p>
            </div>
        </div>
    ),
});

interface VietnamMapProps {
    mergerUnits?: MergerUnit[];
    onSelectUnit?: (unit: MergerUnit) => void;
    highlightedProvinceNames?: string[];
}

export default function VietnamMap({
    mergerUnits = [],
    onSelectUnit,
    highlightedProvinceNames = []
}: VietnamMapProps) {
    const [mapData, setMapData] = useState<VietnamMapData | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        async function loadData() {
            try {
                setLoading(true);
                const data = await loadVietnamMapData();
                setMapData(data);
            } catch (err) {
                console.error('Error loading map data:', err);
                setError('Failed to load map data');
            } finally {
                setLoading(false);
            }
        }

        loadData();
    }, []);

    if (loading) {
        return (
            <div className="w-full h-screen flex items-center justify-center bg-slate-100">
                <div className="text-center">
                    <div className="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
                    <p className="text-slate-600 font-medium">Đang tải bản đồ...</p>
                </div>
            </div>
        );
    }

    if (error || !mapData) {
        return (
            <div className="w-full h-screen flex items-center justify-center bg-slate-100">
                <div className="text-center text-red-600">
                    <p className="text-xl font-bold mb-2">⚠️ Lỗi tải dữ liệu</p>
                    <p>{error || 'Không có dữ liệu bản đồ'}</p>
                </div>
            </div>
        );
    }

    return (
        <MapComponent
            mapData={mapData}
            mergerUnits={mergerUnits}
            onSelectUnit={onSelectUnit}
            highlightedProvinceNames={highlightedProvinceNames}
        />
    );
}
