import React, { useState } from 'react';
import { SidePanelProps as BaseSidePanelProps } from '../types/map';
import { MergerUnit, ProvinceListItem } from '../types/merger';

interface SidePanelProps extends BaseSidePanelProps {
    isMergerMode?: boolean;
    provinceList?: ProvinceListItem[];
    selectedProvinceId?: string | null;
    onSelectProvince?: (id: string) => void;
    mergerUnits?: MergerUnit[];
    loading?: boolean;
    onSelectUnit?: (unit: MergerUnit) => void;
}

export default function SidePanel({
    isOpen,
    province,
    wards,
    onClose,
    isMergerMode = false,
    provinceList = [],
    selectedProvinceId,
    onSelectProvince,
    mergerUnits = [],
    loading = false,
    onSelectUnit
}: SidePanelProps) {
    const [expandedUnitId, setExpandedUnitId] = useState<number | null>(null);
    const [searchTerm, setSearchTerm] = useState('');

    const toggleExpand = (id: number) => {
        setExpandedUnitId(expandedUnitId === id ? null : id);
    };

    const filteredUnits = mergerUnits.filter(u =>
        u.tenhc.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.truocsapnhap.toLowerCase().includes(searchTerm.toLowerCase())
    );

    return (
        <>
            {/* Backdrop */}
            {isOpen && (
                <div
                    className="fixed inset-0 bg-black bg-opacity-30 z-40 transition-opacity duration-200"
                    onClick={onClose}
                />
            )}

            {/* Panel */}
            <div
                className={`fixed top-0 right-0 h-full w-[400px] bg-white shadow-2xl z-50 transform transition-transform duration-300 ease-in-out ${isOpen ? 'translate-x-0' : 'translate-x-full'
                    }`}
            >
                <div className="h-full flex flex-col">
                    {/* Header */}
                    <div className="bg-gradient-to-r from-blue-600 to-blue-500 px-6 py-5 shadow-md">
                        <div className="flex items-center justify-between text-white">
                            <h2 className="text-xl font-bold">
                                {isMergerMode ? 'Thông tin Sáp nhập' : (province?.name_with_type || 'Thông tin tỉnh')}
                            </h2>
                            <button
                                onClick={onClose}
                                className="text-white/80 hover:text-white transition-colors duration-200"
                                aria-label="Đóng"
                            >
                                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </div>
                    </div>

                    {/* Content */}
                    <div className="flex-1 overflow-y-auto bg-gray-50">
                        {isMergerMode ? (
                            <div className="p-4 space-y-4">
                                {/* Province Selector */}
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-1">Chọn Tỉnh/Thành phố</label>
                                    <select
                                        className="w-full p-2 border border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                                        value={selectedProvinceId || ''}
                                        onChange={(e) => onSelectProvince?.(e.target.value)}
                                    >
                                        <option value="">-- Chọn tỉnh --</option>
                                        {provinceList.map(p => (
                                            <option key={p.mahc} value={p.mahc}>{p.tentinh}</option>
                                        ))}
                                    </select>
                                </div>

                                {loading ? (
                                    <div className="flex justify-center py-8">
                                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                                    </div>
                                ) : selectedProvinceId && (
                                    <>
                                        {/* Search */}
                                        <div className="relative">
                                            <input
                                                type="text"
                                                placeholder="Tìm kiếm đơn vị hành chính..."
                                                className="w-full p-2 pl-8 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                                                value={searchTerm}
                                                onChange={(e) => setSearchTerm(e.target.value)}
                                            />
                                            <svg className="w-4 h-4 text-gray-400 absolute left-2.5 top-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                                            </svg>
                                        </div>

                                        {/* List */}
                                        <div className="space-y-3">
                                            <div className="flex justify-between items-center text-sm text-gray-500">
                                                <span>Danh sách đơn vị mới ({filteredUnits.length})</span>
                                            </div>

                                            {filteredUnits.map((unit) => (
                                                <div
                                                    key={unit.id}
                                                    className={`bg-white rounded-lg shadow-sm border transition-all duration-200 ${expandedUnitId === unit.id ? 'ring-2 ring-blue-500 border-transparent' : 'border-gray-200 hover:border-blue-300'}`}
                                                >
                                                    <div
                                                        className="p-3 cursor-pointer flex justify-between items-start"
                                                        onClick={() => {
                                                            toggleExpand(unit.id);
                                                            onSelectUnit?.(unit);
                                                        }}
                                                    >
                                                        <div>
                                                            <h4 className="font-bold text-gray-800 flex items-center gap-2">
                                                                {unit.loai} {unit.tenhc}
                                                                {expandedUnitId === unit.id && <span className="text-xs bg-blue-100 text-blue-800 px-2 py-0.5 rounded-full">Đang xem</span>}
                                                            </h4>
                                                            <p className="text-xs text-gray-500 mt-1">Dân số: {Number(unit.dansonguoi).toLocaleString()} | Diện tích: {unit.dientichkm2} km²</p>
                                                        </div>
                                                        <button className="text-gray-400 hover:text-blue-600">
                                                            <svg className={`w-5 h-5 transform transition-transform ${expandedUnitId === unit.id ? 'rotate-180' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                                                            </svg>
                                                        </button>
                                                    </div>

                                                    {/* Expanded Details */}
                                                    <div className="px-3 pb-3 pt-0 border-t border-gray-100 mt-2 bg-blue-50/50 rounded-b-lg">
                                                        <div className="mt-3">
                                                            <h5 className="text-xs font-bold text-blue-600 uppercase tracking-wider mb-1 flex items-center gap-1">
                                                                <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" /></svg>
                                                                Các đơn vị cũ (Sáp nhập từ):
                                                            </h5>
                                                            <div className="text-sm text-gray-700 leading-relaxed bg-white p-3 rounded border border-blue-200 shadow-sm">
                                                                {unit.truocsapnhap.split(',').map((item, idx) => (
                                                                    <div key={idx} className="mb-1 last:mb-0 flex items-start gap-2">
                                                                        <span className="text-blue-400 mt-1.5">•</span>
                                                                        <span>{item.trim()}</span>
                                                                    </div>
                                                                ))}
                                                            </div>
                                                        </div>
                                                        <div className="mt-3 grid grid-cols-2 gap-2 text-xs text-gray-600">
                                                            <div>
                                                                <span className="font-medium text-gray-500">Trung tâm HC:</span>
                                                                <div className="mt-0.5 font-semibold text-gray-800">{unit.trungtamhc}</div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            ))}

                                            {filteredUnits.length === 0 && (
                                                <div className="text-center py-8 text-gray-500">
                                                    Không tìm thấy kết quả
                                                </div>
                                            )}
                                        </div>
                                    </>
                                )}
                            </div>
                        ) : (
                            // Original Content Logic
                            <div className="px-6 py-4">
                                {province && (
                                    <>
                                        <div className="mb-6">
                                            <h3 className="text-sm font-medium text-gray-500 mb-2">Mã tỉnh</h3>
                                            <p className="text-base text-gray-800">{province.code}</p>
                                        </div>
                                        <div>
                                            <h3 className="text-sm font-medium text-gray-500 mb-3">
                                                Danh sách xã/phường ({wards.length})
                                            </h3>
                                            {wards.length > 0 ? (
                                                <div className="space-y-2">
                                                    {wards.map((ward) => (
                                                        <div
                                                            key={ward.code}
                                                            className="p-3 bg-white rounded-lg hover:bg-blue-50 transition-colors duration-200 border border-gray-200 shadow-sm"
                                                        >
                                                            <p className="text-sm font-medium text-gray-800">{ward.name_with_type}</p>
                                                            <p className="text-xs text-gray-500 mt-1">Mã: {ward.code}</p>
                                                        </div>
                                                    ))}
                                                </div>
                                            ) : (
                                                <p className="text-sm text-gray-500 italic">Không có dữ liệu xã/phường</p>
                                            )}
                                        </div>
                                    </>
                                )}
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </>
    );
}
