// Province data structure
export interface Province {
  code: string;
  name: string;
  slug: string;
  type: string;
  name_with_type: string;
}

// Ward data structure
export interface Ward {
  code: string;
  name: string;
  type: string;
  slug: string;
  name_with_type: string;
  path: string;
  path_with_type: string;
  parent_code: string;
}

// Province map with SVG path
export interface ProvinceMap {
  code: string;
  name: string;
  path: string;
}

// Tooltip state
export interface TooltipState {
  visible: boolean;
  x: number;
  y: number;
  content: string;
}

// Component props
export interface VietnamMapProps {
  provinces: Record<string, Province>;
  wards: Record<string, Ward>;
  provinceMaps: Record<string, ProvinceMap>;
  onProvinceClick: (provinceCode: string) => void;
}

export interface TooltipProps {
  visible: boolean;
  x: number;
  y: number;
  content: string;
}

export interface SidePanelProps {
  isOpen: boolean;
  province: Province | null;
  wards: Ward[];
  onClose: () => void;
}

export interface VietnamMapData {
  provinces63: any; // GeoJSON
  districts: any; // GeoJSON
  provinces34: any; // JSON or GeoJSON
  wards: any; // JSON
}

export interface MapState {
  viewMode: '63-provinces' | '34-provinces';
  selectedProvince: string | null;
  hoveredProvince: string | null;
  showDistricts: boolean;
  showWards: boolean;
}
