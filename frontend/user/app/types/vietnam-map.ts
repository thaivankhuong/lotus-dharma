// Type definitions for Vietnam Map data

export interface ProvinceData {
    name: string;
    slug: string;
    type: string;
    name_with_type: string;
    code: string;
}

export interface WardData {
    name: string;
    type: string;
    slug: string;
    name_with_type: string;
    path: string;
    path_with_type: string;
    code: string;
    parent_code: string;
}

export interface GeoJSONProperties {
    shapeName: string;
    shapeISO: string;
    shapeID: string;
    shapeGroup: string;
    shapeType: string;
}

export interface ProvinceFeature {
    type: 'Feature';
    properties: GeoJSONProperties;
    geometry: {
        type: 'Polygon' | 'MultiPolygon';
        coordinates: number[][][] | number[][][][];
    };
}

export interface VietnamGeoJSON {
    type: 'FeatureCollection';
    features: ProvinceFeature[];
}

export interface MapState {
    viewMode: '63-provinces' | '34-provinces';
    selectedProvince: string | null;
    hoveredProvince: string | null;
    showDistricts: boolean;
    showWards: boolean;
}

export interface VietnamMapData {
    provinces63: VietnamGeoJSON;
    districts: VietnamGeoJSON;
    provinces34: Record<string, ProvinceData>;
    wards: Record<string, WardData>;
}
