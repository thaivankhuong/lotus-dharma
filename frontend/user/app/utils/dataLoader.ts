import { Province, Ward, ProvinceMap } from '../types/map';

/**
 * Load province data from public folder
 */
export async function loadProvinces(): Promise<Record<string, Province>> {
    try {
        const response = await fetch('/data/province.json');
        if (!response.ok) {
            throw new Error('Failed to load province data');
        }
        const data = await response.json();
        return data;
    } catch (error) {
        console.error('Error loading provinces:', error);
        return {};
    }
}

/**
 * Load ward data from public folder
 */
export async function loadWards(): Promise<Record<string, Ward>> {
    try {
        const response = await fetch('/data/ward.json');
        if (!response.ok) {
            throw new Error('Failed to load ward data');
        }
        const data = await response.json();
        return data;
    } catch (error) {
        console.error('Error loading wards:', error);
        return {};
    }
}

/**
 * Load Vietnam map SVG paths from public folder
 */
export async function loadVietnamMap(): Promise<Record<string, ProvinceMap>> {
    try {
        const response = await fetch('/data/vietnam-map.json');
        if (!response.ok) {
            throw new Error('Failed to load Vietnam map data');
        }
        const data = await response.json();
        return data;
    } catch (error) {
        console.error('Error loading Vietnam map:', error);
        return {};
    }
}

/**
 * Filter wards by province code
 */
export function getWardsByProvince(
    wards: Record<string, Ward>,
    provinceCode: string
): Ward[] {
    return Object.values(wards).filter(
        (ward) => ward.parent_code === provinceCode
    );
}

/**
 * Get province by code
 */
export function getProvinceByCode(
    provinces: Record<string, Province>,
    code: string
): Province | null {
    return provinces[code] || null;
}
