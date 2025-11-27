'use client';

import React, { useEffect, useState } from 'react';
import VietnamMap from './components/VietnamMap';
import SidePanel from './components/SidePanel';
import { Province, Ward, ProvinceMap } from './types/map';
import { useMergerData } from './hooks/useMergerData';
import {
  loadProvinces,
  loadWards,
  loadVietnamMap,
  getWardsByProvince,
  getProvinceByCode,
} from './utils/dataLoader';

export default function Home() {
  const [provinces, setProvinces] = useState<Record<string, Province>>({});
  const [wards, setWards] = useState<Record<string, Ward>>({});
  const [provinceMaps, setProvinceMaps] = useState<Record<string, ProvinceMap>>({});
  const [selectedProvinceCode, setSelectedProvinceCode] = useState<string | null>(null);
  const [isPanelOpen, setIsPanelOpen] = useState(false);
  const [loading, setLoading] = useState(true);

  // Merger Data Hook
  const {
    provinces: mergerProvinces,
    selectedProvinceId,
    setSelectedProvinceId,
    mergerUnits,
    loading: mergerLoading
  } = useMergerData();
  const [isMergerMode, setIsMergerMode] = useState(true);

  // Load data on mount
  useEffect(() => {
    async function fetchData() {
      setLoading(true);
      try {
        const [provincesData, wardsData, mapData] = await Promise.all([
          loadProvinces(),
          loadWards(),
          loadVietnamMap(),
        ]);

        setProvinces(provincesData);
        setWards(wardsData);
        setProvinceMaps(mapData);
      } catch (error) {
        console.error('Error loading data:', error);
      } finally {
        setLoading(false);
      }
    }

    fetchData();
  }, []);

  const handleProvinceClick = (provinceCode: string) => {
    setSelectedProvinceCode(provinceCode);
    setIsPanelOpen(true);
  };

  const handlePanelClose = () => {
    setIsPanelOpen(false);
    setTimeout(() => {
      setSelectedProvinceCode(null);
    }, 300); // Wait for animation to complete
  };

  const selectedProvince = selectedProvinceCode
    ? getProvinceByCode(provinces, selectedProvinceCode)
    : null;

  const selectedWards = selectedProvinceCode
    ? getWardsByProvince(wards, selectedProvinceCode)
    : [];

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-amber-50 via-orange-50 to-yellow-50">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-16 w-16 border-4 border-amber-200 border-t-amber-600 mb-4"></div>
          <p className="text-lg text-gray-600 font-medium">Đang tải bản đồ...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-white">
      {/* Header */}
      <header className="bg-white/90 backdrop-blur-md shadow-sm border-b border-slate-200 sticky top-0 z-30">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              {/* Logo */}
              <div className="flex items-center justify-center w-12 h-12 bg-gradient-to-br from-blue-600 to-indigo-700 rounded-xl shadow-lg text-white">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
                </svg>
              </div>

              {/* Title */}
              <div>
                <h1 className="text-2xl font-bold text-slate-800 tracking-tight">
                  Bản Đồ Sáp Nhập Đơn Vị Hành Chính
                </h1>
                <p className="text-sm text-slate-500 mt-0.5">
                  Tra cứu thông tin sáp nhập xã/phường mới nhất
                </p>
              </div>
            </div>

            {/* Toggle Mode */}
            <div className="flex items-center bg-slate-100 rounded-lg p-1 border border-slate-200">
              <button
                onClick={() => setIsMergerMode(false)}
                className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${!isMergerMode ? 'bg-white text-slate-800 shadow-sm' : 'text-slate-500 hover:text-slate-700'}`}
              >
                Bản đồ cũ
              </button>
              <button
                onClick={() => {
                  setIsMergerMode(true);
                  setIsPanelOpen(true);
                }}
                className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${isMergerMode ? 'bg-blue-600 text-white shadow-md' : 'text-slate-500 hover:text-slate-700'}`}
              >
                Sáp nhập mới
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-6 py-6 h-[calc(100vh-100px)]">
        <div className="bg-white rounded-2xl shadow-xl overflow-hidden border border-slate-200 h-full flex flex-col">
          {/* Info Banner */}
          <div className="bg-blue-50/50 px-6 py-3 border-b border-blue-100 flex justify-between items-center">
            <p className="text-sm text-slate-700">
              <span className="font-semibold text-blue-700">💡 Hướng dẫn:</span> {isMergerMode ? 'Chọn tỉnh từ menu bên phải để xem chi tiết sáp nhập' : 'Di chuột qua các tỉnh để xem tên, nhấp vào để xem chi tiết'}
            </p>
            {isMergerMode && (
              <button
                onClick={() => setIsPanelOpen(true)}
                className="text-sm font-medium text-blue-600 hover:text-blue-800 flex items-center gap-1"
              >
                <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16m-7 6h7" />
                </svg>
                Mở danh sách
              </button>
            )}
          </div>

          {/* Map Container */}
          <div className="flex-1 relative bg-slate-50">
            <VietnamMap
              mergerUnits={isMergerMode ? mergerUnits : []}
              highlightedProvinceNames={isMergerMode ? mergerProvinces.map(p => p.tentinh) : []}
              onSelectUnit={(unit) => {
                setIsPanelOpen(true);
              }}
            />
          </div>
        </div>
      </main>

      {/* Side Panel */}
      <SidePanel
        isOpen={isPanelOpen}
        onClose={handlePanelClose}
        // Old props
        province={selectedProvince}
        wards={selectedWards}
        // Merger props
        isMergerMode={isMergerMode}
        provinceList={mergerProvinces}
        selectedProvinceId={selectedProvinceId}
        onSelectProvince={setSelectedProvinceId}
        mergerUnits={mergerUnits}
        loading={mergerLoading}
      />
    </div>
  );
}
