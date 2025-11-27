import { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import * as turf from '@turf/turf';
import 'leaflet/dist/leaflet.css';
import type { VietnamMapData, MapState } from '../types/map';
import { MergerUnit } from '../types/merger';

// Fix Leaflet default icon issue
// @ts-ignore
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
    iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
    shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

// Helper to normalize string for comparison
function normalizeName(name: string): string {
    if (!name) return '';
    return name.toLowerCase()
        .normalize("NFD").replace(/[\u0300-\u036f]/g, "")
        .replace(/đ/g, "d")
        .replace(/[^a-z0-9]/g, "");
}

interface Props {
    mapData: VietnamMapData;
    mergerUnits?: MergerUnit[];
    onSelectUnit?: (unit: MergerUnit) => void;
    highlightedProvinceNames?: string[];
}

export default function VietnamMapClient({
    mapData,
    mergerUnits = [],
    onSelectUnit,
    highlightedProvinceNames = []
}: Props) {
    const mapRef = useRef<L.Map | null>(null);
    const mapContainerRef = useRef<HTMLDivElement>(null);
    const provincesLayerRef = useRef<L.GeoJSON | null>(null);
    const districtsLayerRef = useRef<L.GeoJSON | null>(null);
    const mergerLayerRef = useRef<L.LayerGroup | null>(null);

    const [mapState, setMapState] = useState<MapState>({
        viewMode: '63-provinces',
        selectedProvince: null,
        hoveredProvince: null,
        showDistricts: false,
        showWards: false,
    });

    // Initialize map
    useEffect(() => {
        if (!mapContainerRef.current || mapRef.current) return;

        const map = L.map(mapContainerRef.current, {
            center: [16.0, 106.0],
            zoom: 6,
            minZoom: 5,
            maxZoom: 18,
            zoomControl: true,
        });

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 18,
        }).addTo(map);

        mapRef.current = map;

        // Initialize merger layer group
        mergerLayerRef.current = L.layerGroup().addTo(map);

        // Handle resize to prevent UI jumping
        const resizeObserver = new ResizeObserver(() => {
            map.invalidateSize();
        });
        resizeObserver.observe(mapContainerRef.current);

        return () => {
            resizeObserver.disconnect();
            map.remove();
            mapRef.current = null;
        };
    }, []);

    // Render Merger Units Markers
    useEffect(() => {
        if (!mapRef.current || !mergerLayerRef.current) return;

        // Clear existing markers
        mergerLayerRef.current.clearLayers();

        if (mergerUnits.length === 0) return;

        const bounds = L.latLngBounds([]);

        mergerUnits.forEach(unit => {
            if (unit.vido && unit.kinhdo) {
                // Create a custom pulse marker
                const pulseIcon = L.divIcon({
                    className: 'css-icon',
                    html: `<div class="gps_ring"></div>`,
                    iconSize: [20, 20],
                    iconAnchor: [10, 10]
                });

                const marker = L.marker([unit.vido, unit.kinhdo], {
                    icon: pulseIcon
                });

                const popupContent = `
                    <div class="p-2 min-w-[200px]">
                        <div class="font-bold text-blue-600 mb-1">${unit.loai} ${unit.tenhc}</div>
                        <div class="text-xs text-slate-600 mb-2">Thuộc: ${unit.tentinh}</div>
                        <div class="text-xs border-t pt-1 mt-1">
                            <div>Diện tích: <b>${unit.dientichkm2} km²</b></div>
                            <div>Dân số: <b>${unit.dansonguoi} người</b></div>
                        </div>
                        <button class="mt-2 w-full bg-blue-500 text-white text-xs py-1 px-2 rounded hover:bg-blue-600 transition-colors" 
                                onclick="window.dispatchEvent(new CustomEvent('select-merger-unit', { detail: ${unit.id} }))">
                            Xem chi tiết
                        </button>
                    </div>
                `;

                marker.bindPopup(popupContent);

                marker.on('click', () => {
                    onSelectUnit?.(unit);
                });

                marker.addTo(mergerLayerRef.current!);
                bounds.extend([unit.vido, unit.kinhdo]);
            }
        });

        // Add global event listener for popup button
        const handlePopupClick = (e: any) => {
            const unitId = e.detail;
            const unit = mergerUnits.find(u => u.id === unitId);
            if (unit) onSelectUnit?.(unit);
        };
        window.addEventListener('select-merger-unit', handlePopupClick);

        if (bounds.isValid()) {
            // Fit bounds but keep some padding
            mapRef.current.fitBounds(bounds, { padding: [50, 50], maxZoom: 10 });
        }

        return () => {
            window.removeEventListener('select-merger-unit', handlePopupClick);
        };

    }, [mergerUnits, onSelectUnit]);

    // Handle province click
    const handleProvinceClick = (feature: any) => {
        if (!mapRef.current || !mapData) return;

        const provinceName = feature.properties.shapeName;
        console.log('Clicked province:', provinceName);

        setMapState((prev) => ({
            ...prev,
            selectedProvince: provinceName,
            showDistricts: true,
        }));

        // Zoom to province
        const layer = provincesLayerRef.current?.getLayers().find((l: any) => {
            return l.feature?.properties.shapeName === provinceName;
        });

        if (layer) {
            const bounds = (layer as any).getBounds();
            mapRef.current.fitBounds(bounds, { padding: [50, 50] });
        }

        // Load districts
        loadDistrictsForProvince(provinceName, feature);
    };

    // Load districts using Turf.js
    const loadDistrictsForProvince = (provinceName: string, provinceFeature: any) => {
        if (!mapRef.current || !mapData) return;

        if (districtsLayerRef.current) {
            districtsLayerRef.current.remove();
        }

        try {
            const provincePoly = turf.feature(provinceFeature.geometry);

            const provinceDistricts = mapData.districts.features.filter((districtFeature: any) => {
                try {
                    const districtCentroid = turf.centroid(districtFeature.geometry as any);
                    return turf.booleanPointInPolygon(districtCentroid, provincePoly as any);
                } catch (error) {
                    return false;
                }
            });

            console.log(`Found ${provinceDistricts.length} districts for ${provinceName}`);

            if (provinceDistricts.length > 0) {
                const districtsLayer = L.geoJSON(
                    {
                        type: 'FeatureCollection',
                        features: provinceDistricts,
                    } as any,
                    {
                        style: {
                            fillColor: 'transparent',
                            color: '#ef4444',
                            weight: 1.5,
                            opacity: 0.7,
                        },
                        onEachFeature: (feature: any, layer: any) => {
                            layer.bindTooltip(feature.properties.shapeName, {
                                permanent: false,
                                direction: 'center',
                                className: 'district-tooltip',
                            });
                        },
                    }
                );

                districtsLayer.addTo(mapRef.current);
                districtsLayerRef.current = districtsLayer;
            }
        } catch (error) {
            console.error('Error loading districts:', error);
        }
    };

    // Render provinces layer
    useEffect(() => {
        if (!mapRef.current || !mapData) return;

        if (provincesLayerRef.current) {
            provincesLayerRef.current.remove();
        }

        const provinceStyle = (feature: any): L.PathOptions => {
            const provinceName = feature?.properties.shapeName;

            const isHovered = provinceName === mapState.hoveredProvince;
            const isSelected = provinceName === mapState.selectedProvince;

            // Check if highlighted (in merger list) using normalized name
            const normalizedProvName = normalizeName(provinceName);
            const isHighlighted = highlightedProvinceNames.some(name => normalizeName(name) === normalizedProvName);

            let fillColor = '#94a3b8'; // Default gray
            if (isSelected) fillColor = '#3b82f6'; // Blue
            else if (isHovered) fillColor = '#60a5fa'; // Light Blue
            else if (isHighlighted) fillColor = '#f59e0b'; // Amber/Orange for merger provinces

            return {
                fillColor: fillColor,
                fillOpacity: (isSelected || isHighlighted) ? 0.6 : (isHovered ? 0.4 : 0.2),
                color: isSelected ? '#1e40af' : (isHighlighted ? '#d97706' : (isHovered ? '#2563eb' : '#475569')),
                weight: isSelected ? 3 : (isHovered || isHighlighted) ? 2 : 1,
                opacity: 1,
            };
        };

        const provincesLayer = L.geoJSON(mapData.provinces63 as any, {
            style: provinceStyle,
            onEachFeature: (feature: any, layer: any) => {
                const provinceName = feature.properties.shapeName;

                layer.on({
                    mouseover: () => {
                        setMapState((prev) => ({ ...prev, hoveredProvince: provinceName }));
                        // Simple highlight on hover
                        layer.setStyle({
                            weight: 2,
                            color: '#2563eb',
                            fillOpacity: 0.5,
                        });
                    },
                    mouseout: () => {
                        setMapState((prev) => ({ ...prev, hoveredProvince: null }));
                        provincesLayer.resetStyle(layer);
                    },
                    click: () => {
                        handleProvinceClick(feature);
                    },
                });

                layer.bindTooltip(provinceName, {
                    permanent: false,
                    direction: 'top',
                    offset: [0, -10],
                    className: 'province-tooltip',
                    sticky: true, // Follow cursor
                });
            },
        });

        provincesLayer.addTo(mapRef.current);
        provincesLayerRef.current = provincesLayer;

        // Only fit bounds if NOT showing merger units (to avoid conflict)
        if (mergerUnits.length === 0) {
            mapRef.current.fitBounds(provincesLayer.getBounds());
        }
    }, [mapData, mapState.hoveredProvince, mapState.selectedProvince, mergerUnits.length, highlightedProvinceNames]);

    // Reset view
    const handleResetView = () => {
        if (!mapRef.current || !provincesLayerRef.current) return;

        setMapState({
            viewMode: '63-provinces',
            selectedProvince: null,
            hoveredProvince: null,
            showDistricts: false,
            showWards: false,
        });

        if (districtsLayerRef.current) {
            districtsLayerRef.current.remove();
            districtsLayerRef.current = null;
        }

        // Clear merger markers if any
        if (mergerLayerRef.current) {
            mergerLayerRef.current.clearLayers();
        }

        mapRef.current.fitBounds(provincesLayerRef.current.getBounds());
    };

    return (
        <div className="relative w-full h-full">
            <div ref={mapContainerRef} className="w-full h-full" />

            {/* Controls Panel */}
            <div className="absolute top-4 right-4 bg-white/95 backdrop-blur-sm rounded-lg shadow-xl p-4 z-[1000] min-w-[250px]">
                <h3 className="text-lg font-bold text-slate-800 mb-3">Bản đồ Việt Nam</h3>

                <div className="mb-4">
                    <label className="block text-sm font-medium text-slate-700 mb-2">Chế độ xem</label>
                    <div className="flex gap-2">
                        <button
                            onClick={() => setMapState((prev) => ({ ...prev, viewMode: '63-provinces' }))}
                            className={`flex-1 px-3 py-2 rounded-md text-sm font-medium transition-colors ${mapState.viewMode === '63-provinces'
                                ? 'bg-blue-600 text-white'
                                : 'bg-slate-200 text-slate-700 hover:bg-slate-300'
                                }`}
                        >
                            63 tỉnh cũ
                        </button>
                        <button
                            onClick={() => setMapState((prev) => ({ ...prev, viewMode: '34-provinces' }))}
                            className={`flex-1 px-3 py-2 rounded-md text-sm font-medium transition-colors ${mapState.viewMode === '34-provinces'
                                ? 'bg-blue-600 text-white'
                                : 'bg-slate-200 text-slate-700 hover:bg-slate-300'
                                }`}
                            disabled
                            title="Chức năng đang phát triển"
                        >
                            34 tỉnh mới
                        </button>
                    </div>
                </div>

                {mapState.selectedProvince && (
                    <div className="mb-4 p-3 bg-blue-50 rounded-md border border-blue-200">
                        <p className="text-sm font-medium text-blue-900">Đã chọn:</p>
                        <p className="text-lg font-bold text-blue-700">{mapState.selectedProvince}</p>
                    </div>
                )}

                <button
                    onClick={handleResetView}
                    className="w-full px-4 py-2 bg-slate-600 hover:bg-slate-700 text-white rounded-md font-medium transition-colors"
                >
                    Reset View
                </button>

                {mapData && (
                    <div className="mt-4 pt-4 border-t border-slate-200">
                        <p className="text-xs text-slate-600">Tỉnh thành: {mapData.provinces63.features.length}</p>
                        <p className="text-xs text-slate-600">
                            Phường/Xã: {Object.keys(mapData.wards).length.toLocaleString()}
                        </p>
                    </div>
                )}
            </div>

            {/* Legend */}
            <div className="absolute bottom-4 left-4 bg-white/95 backdrop-blur-sm rounded-lg shadow-xl p-4 z-[1000]">
                <h4 className="text-sm font-bold text-slate-800 mb-2">Chú thích</h4>
                <div className="space-y-2 text-xs">
                    <div className="flex items-center gap-2">
                        <div className="w-4 h-4 bg-slate-400/20 border border-slate-600"></div>
                        <span className="text-slate-700">Tỉnh thành</span>
                    </div>
                    <div className="flex items-center gap-2">
                        <div className="w-4 h-4 bg-blue-400/40 border-2 border-blue-700"></div>
                        <span className="text-slate-700">Đã chọn</span>
                    </div>
                    {highlightedProvinceNames.length > 0 && (
                        <div className="flex items-center gap-2">
                            <div className="w-4 h-4 bg-amber-500/60 border-2 border-amber-700"></div>
                            <span className="text-slate-700">Có sáp nhập</span>
                        </div>
                    )}
                    {mergerUnits.length > 0 && (
                        <div className="flex items-center gap-2">
                            <div className="w-4 h-4 rounded-full bg-red-500 border-2 border-white shadow-sm"></div>
                            <span className="text-slate-700">Đơn vị mới</span>
                        </div>
                    )}
                </div>
            </div>

            <style jsx global>{`
        .province-tooltip,
        .district-tooltip {
          background-color: rgba(30, 41, 59, 0.95);
          border: none;
          border-radius: 8px;
          color: white;
          font-weight: 600;
          padding: 8px 14px;
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
          font-size: 13px;
          transition: opacity 0.2s ease-in-out;
          pointer-events: none;
          white-space: nowrap;
        }
        
        .leaflet-tooltip {
          animation: tooltipFadeIn 0.2s ease-in-out;
        }
        
        @keyframes tooltipFadeIn {
          from {
            opacity: 0;
            transform: translateY(-5px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
        
        .leaflet-tooltip-top:before {
          border-top-color: rgba(30, 41, 59, 0.95) !important;
        }
        
        .leaflet-tooltip-bottom:before {
          border-bottom-color: rgba(30, 41, 59, 0.95) !important;
        }
        
        .leaflet-tooltip-left:before {
          border-left-color: rgba(30, 41, 59, 0.95) !important;
        }
        
        .leaflet-tooltip-right:before {
          border-right-color: rgba(30, 41, 59, 0.95) !important;
        }
        
        /* Pulse Animation for Markers */
        .css-icon {

        }
        .gps_ring {
            border: 3px solid #ef4444;
            -webkit-border-radius: 30px;
            height: 18px;
            width: 18px;
            -webkit-animation: pulsate 1s ease-out;
            -webkit-animation-iteration-count: infinite; 
            opacity: 0.0;
            background-color: #ef4444;
            border-radius: 50%;
        }
        @keyframes pulsate {
            0% {-webkit-transform: scale(0.1, 0.1); opacity: 0.0;}
            50% {opacity: 1.0;}
            100% {-webkit-transform: scale(1.2, 1.2); opacity: 0.0;}
        }
      `}</style>
        </div>
    );
}
