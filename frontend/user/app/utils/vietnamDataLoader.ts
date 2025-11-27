// Data loader for Vietnam map data
import type { VietnamGeoJSON, ProvinceData, WardData, VietnamMapData } from '../types/vietnam-map';

/**
 * Load all Vietnam map data files
 */
export async function loadVietnamMapData(): Promise<VietnamMapData> {
    try {
        // Load all data files in parallel
        const t = Date.now();
        const [provinces63Response, districtsResponse, provinces34Response, wardsResponse] = await Promise.all([
            fetch(`/documents/data-province-vietnam/geoBoundaries-VNM-ADM1.geojson?t=${t}`),
            fetch(`/documents/data-province-vietnam/geoBoundaries-VNM-ADM2.geojson?t=${t}`),
            fetch(`/documents/data-province-vietnam/province.json?t=${t}`),
            fetch(`/documents/data-province-vietnam/ward.json?t=${t}`),
        ]);

        // Check if all requests were successful
        if (!provinces63Response.ok) throw new Error('Failed to load provinces (ADM1) data');
        if (!districtsResponse.ok) throw new Error('Failed to load districts (ADM2) data');
        if (!provinces34Response.ok) throw new Error('Failed to load provinces (2025) data');
        if (!wardsResponse.ok) throw new Error('Failed to load wards data');

        // Parse JSON data
        const [provinces63, districts, provinces34, wards] = await Promise.all([
            provinces63Response.json() as Promise<VietnamGeoJSON>,
            districtsResponse.json() as Promise<VietnamGeoJSON>,
            provinces34Response.json() as Promise<Record<string, ProvinceData>>,
            wardsResponse.json() as Promise<Record<string, WardData>>,
        ]);

        console.log('✅ Loaded Vietnam map data:', {
            provinces63Count: provinces63.features.length,
            districtsCount: districts.features.length,
            provinces34Count: Object.keys(provinces34).length,
            wardsCount: Object.keys(wards).length,
        });

        return {
            provinces63,
            districts,
            provinces34,
            wards,
        };
    } catch (error) {
        console.error('❌ Error loading Vietnam map data:', error);
        throw error;
    }
}

/**
 * Get wards for a specific province
 */
export function getWardsByProvinceCode(
    wards: Record<string, WardData>,
    provinceCode: string
): WardData[] {
    return Object.values(wards).filter((ward) => ward.parent_code === provinceCode);
}

/**
 * Get districts for a specific province from GeoJSON
 */
export function getDistrictsByProvinceName(
    districts: VietnamGeoJSON,
    provinceName: string
): VietnamGeoJSON['features'] {
    // ADM2 data might have province name in properties
    // We'll need to check the actual structure
    return districts.features.filter((feature) => {
        // This is a placeholder - need to check actual ADM2 structure
        return feature.properties.shapeName?.includes(provinceName);
    });
}

/**
 * Normalize province name for matching
 */
export function normalizeProvinceName(name: string): string {
    return name
        .toLowerCase()
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '') // Remove diacritics
        .replace(/đ/g, 'd')
        .trim();
}

/**
 * Find province feature by name
 */
export function findProvinceByName(
    geojson: VietnamGeoJSON,
    provinceName: string
): VietnamGeoJSON['features'][0] | undefined {
    const normalized = normalizeProvinceName(provinceName);
    return geojson.features.find(
        (feature) => normalizeProvinceName(feature.properties.shapeName) === normalized
    );
}
