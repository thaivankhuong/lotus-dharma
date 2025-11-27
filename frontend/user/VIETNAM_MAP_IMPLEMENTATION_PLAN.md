# 🗺️ VIETNAM MAP IMPLEMENTATION PLAN
## Tham khảo: https://sapnhap.bando.com.vn/

---

## 📊 PHÂN TÍCH TRANG WEB MẪU

### 🎯 Tính Năng Chính
1. **Bản đồ 34 tỉnh/thành phố mới** (sau sáp nhập)
2. **Click vào tỉnh → Zoom + Hiển thị đường biên giới xã/phường**
3. **Bảng thống kê** bên phải hiển thị danh sách xã/phường
4. **Tìm kiếm** xã/phường theo tên
5. **Màu sắc phân biệt** các tỉnh rõ ràng
6. **Tooltip** hiển thị tên tỉnh khi hover

### 🔍 Phân Tích Kỹ Thuật (từ Network Tab)

#### **Data Sources Được Tìm Thấy:**
```
1. vietnam_sapnhap_tinh.geojson
   - Dữ liệu GeoJSON cho 34 tỉnh/thành phố mới
   - Load ngay khi trang web khởi động
   
2. vietnam_sapnhap_xa_[ID].geojson
   - Dữ liệu GeoJSON cho xã/phường của từng tỉnh
   - Load động khi click vào tỉnh
   - ID tương ứng với mã tỉnh (ví dụ: _8.geojson, _9.geojson)
   
3. Tile Layer: OpenStreetMap tiles
   - Bản đồ nền từ OSM
```

#### **Cấu Trúc Dữ Liệu GeoJSON:**
```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "properties": {
        "id": "1000",
        "name": "Hà Nội",
        "name_en": "Ha Noi",
        "type": "Thành phố Trung ương",
        "wards_count": 579
      },
      "geometry": {
        "type": "MultiPolygon",
        "coordinates": [...]
      }
    }
  ]
}
```

---

## 📋 IMPLEMENTATION PLAN

### **PHASE 1: DATA CRAWLING & PROCESSING** 🕷️

#### **Step 1.1: Crawl GeoJSON Data từ sapnhap.bando.com.vn**

**Mục tiêu:** Download tất cả GeoJSON files về local

**Tasks:**
- [ ] **1.1.1** Tạo script Node.js để crawl data
  - File: `scripts/crawl-vietnam-data.js`
  - Download `vietnam_sapnhap_tinh.geojson` (34 tỉnh)
  - Download tất cả `vietnam_sapnhap_xa_[ID].geojson` (xã/phường)
  
- [ ] **1.1.2** Lưu data vào thư mục `public/data/new-provinces/`
  ```
  public/data/new-provinces/
  ├── provinces.geojson          # 34 tỉnh mới
  └── wards/
      ├── province-1000.geojson  # Hà Nội
      ├── province-1001.geojson  # TP.HCM
      └── ...
  ```

**Script Mẫu:**
```javascript
const fs = require('fs');
const https = require('https');

const BASE_URL = 'https://sapnhap.bando.com.vn';

// Download provinces
const downloadFile = (url, dest) => {
  return new Promise((resolve, reject) => {
    const file = fs.createWriteStream(dest);
    https.get(url, (response) => {
      response.pipe(file);
      file.on('finish', () => {
        file.close(resolve);
      });
    }).on('error', (err) => {
      fs.unlink(dest);
      reject(err);
    });
  });
};

// Main crawl function
async function crawlData() {
  // 1. Download provinces
  await downloadFile(
    `${BASE_URL}/vietnam_sapnhap_tinh.geojson`,
    './public/data/new-provinces/provinces.geojson'
  );
  
  // 2. Download wards for each province (ID từ 1000-1033)
  for (let id = 1000; id <= 1033; id++) {
    await downloadFile(
      `${BASE_URL}/vietnam_sapnhap_xa_${id}.geojson`,
      `./public/data/new-provinces/wards/province-${id}.geojson`
    );
  }
}
```

**Ước tính thời gian:** 30-60 phút (tùy tốc độ mạng)

---

#### **Step 1.2: Validate & Clean Data**

**Tasks:**
- [ ] **1.2.1** Kiểm tra tính hợp lệ của GeoJSON
  - Validate JSON structure
  - Kiểm tra coordinates có đúng format không
  - Kiểm tra properties có đầy đủ không

- [ ] **1.2.2** Tạo mapping table
  - File: `public/data/new-provinces/province-mapping.json`
  - Map giữa 64 tỉnh cũ → 34 tỉnh mới
  
**Mapping Table Mẫu:**
```json
{
  "old_to_new": {
    "Hà Nội": "Hà Nội",
    "Hà Tây": "Hà Nội",
    "Hòa Bình": "Hòa Bình",
    "Sơn La": "Sơn La",
    "Điện Biên": "Điện Biên",
    "Lai Châu": "Lai Châu",
    "Lào Cai": "Lào Cai - Yên Bái",
    "Yên Bái": "Lào Cai - Yên Bái",
    ...
  },
  "new_provinces": [
    {
      "id": "1000",
      "name": "Hà Nội",
      "old_provinces": ["Hà Nội", "Hà Tây"],
      "wards_count": 579,
      "color": "#FF6B6B"
    },
    ...
  ]
}
```

**Ước tính thời gian:** 1-2 giờ

---

### **PHASE 2: UPDATE TYPE DEFINITIONS** 📝

#### **Step 2.1: Update TypeScript Types**

**File:** `app/types/vietnam-map.ts`

**Tasks:**
- [ ] **2.1.1** Thêm types cho 34 tỉnh mới
  ```typescript
  export interface NewProvince {
    id: string;
    name: string;
    nameEn: string;
    type: 'Thành phố Trung ương' | 'Tỉnh';
    oldProvinces: string[]; // Danh sách tỉnh cũ được sáp nhập
    wardsCount: number;
    color: string;
    geometry: GeoJSON.MultiPolygon;
  }
  
  export interface Ward {
    id: string;
    name: string;
    nameEn: string;
    type: 'Phường' | 'Xã' | 'Thị trấn' | 'Đặc khu';
    provinceId: string;
    geometry: GeoJSON.Polygon | GeoJSON.MultiPolygon;
  }
  
  export interface NewProvinceGeoJSON {
    type: 'FeatureCollection';
    features: Array<{
      type: 'Feature';
      properties: NewProvince;
      geometry: GeoJSON.MultiPolygon;
    }>;
  }
  ```

**Ước tính thời gian:** 30 phút

---

### **PHASE 3: IMPLEMENT MAP COMPONENTS** 🗺️

#### **Step 3.1: Create New Map Component**

**File:** `app/components/VietnamMapNew/VietnamMapNew.tsx`

**Tasks:**
- [ ] **3.1.1** Tạo component wrapper (giống VietnamMap.tsx hiện tại)
  - Load dữ liệu 34 tỉnh từ `provinces.geojson`
  - Dynamic import client component
  - Loading state

- [ ] **3.1.2** Tạo client component
  - File: `VietnamMapNewClient.tsx`
  - Hiển thị 34 tỉnh với màu sắc phân biệt
  - Click tỉnh → Load ward data động
  - Zoom vào tỉnh được click

**Component Structure:**
```typescript
'use client';

export default function VietnamMapNewClient({ 
  provincesData 
}: { 
  provincesData: NewProvinceGeoJSON 
}) {
  const [selectedProvince, setSelectedProvince] = useState<string | null>(null);
  const [wardsData, setWardsData] = useState<WardGeoJSON | null>(null);
  const [loading, setLoading] = useState(false);
  
  // Load ward data when province is clicked
  const handleProvinceClick = async (provinceId: string) => {
    setLoading(true);
    const response = await fetch(`/data/new-provinces/wards/province-${provinceId}.geojson`);
    const data = await response.json();
    setWardsData(data);
    setSelectedProvince(provinceId);
    setLoading(false);
    
    // Zoom to province bounds
    // ...
  };
  
  return (
    <MapContainer>
      {/* Province layer */}
      <GeoJSON 
        data={provincesData}
        style={(feature) => ({
          fillColor: feature.properties.color,
          weight: 2,
          color: '#333',
          fillOpacity: 0.7
        })}
        onEachFeature={(feature, layer) => {
          layer.on('click', () => handleProvinceClick(feature.properties.id));
        }}
      />
      
      {/* Ward layer (only show when province is selected) */}
      {wardsData && (
        <GeoJSON 
          data={wardsData}
          style={{
            fillColor: 'transparent',
            weight: 1,
            color: '#666',
            fillOpacity: 0
          }}
        />
      )}
    </MapContainer>
  );
}
```

**Ước tính thời gian:** 3-4 giờ

---

#### **Step 3.2: Create Statistics Panel**

**File:** `app/components/VietnamMapNew/StatisticsPanel.tsx`

**Tasks:**
- [ ] **3.2.1** Tạo panel hiển thị thống kê
  - Tổng số tỉnh: 34
  - Tổng số xã/phường: ~3,321
  - Danh sách xã/phường khi click tỉnh

- [ ] **3.2.2** Tạo bảng danh sách xã/phường
  - Sử dụng Tabulator hoặc TanStack Table
  - Tìm kiếm theo tên
  - Sắp xếp theo tên/loại

**Component Structure:**
```typescript
export default function StatisticsPanel({ 
  selectedProvince,
  wardsData 
}: {
  selectedProvince: NewProvince | null;
  wardsData: Ward[] | null;
}) {
  return (
    <div className="statistics-panel">
      <h2>Thống kê</h2>
      <div className="stats">
        <div>Tỉnh/TP: <strong>34</strong></div>
        <div>Xã/Phường: <strong>3,321</strong></div>
      </div>
      
      {selectedProvince && (
        <>
          <h3>{selectedProvince.name}</h3>
          <p>Số xã/phường: {selectedProvince.wardsCount}</p>
          
          {wardsData && (
            <WardsTable data={wardsData} />
          )}
        </>
      )}
    </div>
  );
}
```

**Ước tính thời gian:** 2-3 giờ

---

#### **Step 3.3: Create Toggle Between Old/New Map**

**File:** `app/components/VietnamMapNew/MapToggle.tsx`

**Tasks:**
- [ ] **3.3.1** Tạo toggle button
  - "63 tỉnh cũ" ↔ "34 tỉnh mới"
  - Switch giữa 2 map components

**Component Structure:**
```typescript
export default function MapToggle() {
  const [mapVersion, setMapVersion] = useState<'old' | 'new'>('new');
  
  return (
    <>
      <div className="map-toggle">
        <button 
          className={mapVersion === 'old' ? 'active' : ''}
          onClick={() => setMapVersion('old')}
        >
          63 tỉnh cũ
        </button>
        <button 
          className={mapVersion === 'new' ? 'active' : ''}
          onClick={() => setMapVersion('new')}
        >
          34 tỉnh mới
        </button>
      </div>
      
      {mapVersion === 'old' ? (
        <VietnamMap />
      ) : (
        <VietnamMapNew />
      )}
    </>
  );
}
```

**Ước tính thời gian:** 1 giờ

---

### **PHASE 4: STYLING & UX** 🎨

#### **Step 4.1: Match Reference Design**

**Tasks:**
- [ ] **4.1.1** Tạo color palette cho 34 tỉnh
  - Mỗi tỉnh có màu riêng biệt
  - Sử dụng HSL để tạo màu hài hòa
  
- [ ] **4.1.2** Style map controls
  - Zoom controls
  - Reset view button
  - Toggle button

- [ ] **4.1.3** Style statistics panel
  - Responsive design
  - Smooth animations
  - Search input styling

**Color Palette Generator:**
```typescript
function generateProvinceColors(count: number): string[] {
  const colors: string[] = [];
  const hueStep = 360 / count;
  
  for (let i = 0; i < count; i++) {
    const hue = i * hueStep;
    const saturation = 60 + (i % 3) * 10; // 60-80%
    const lightness = 50 + (i % 2) * 10;  // 50-60%
    colors.push(`hsl(${hue}, ${saturation}%, ${lightness}%)`);
  }
  
  return colors;
}
```

**Ước tính thời gian:** 2-3 giờ

---

### **PHASE 5: TESTING & OPTIMIZATION** ✅

#### **Step 5.1: Testing**

**Tasks:**
- [ ] **5.1.1** Test trên nhiều tỉnh
  - Click từng tỉnh để kiểm tra ward data
  - Kiểm tra zoom behavior
  
- [ ] **5.1.2** Test performance
  - Load time cho ward data
  - Smooth zoom/pan
  
- [ ] **5.1.3** Test responsive
  - Mobile view
  - Tablet view
  - Desktop view

**Ước tính thời gian:** 2-3 giờ

---

#### **Step 5.2: Optimization**

**Tasks:**
- [ ] **5.2.1** Optimize GeoJSON files
  - Simplify geometries (giảm số points)
  - Compress files
  
- [ ] **5.2.2** Lazy loading
  - Chỉ load ward data khi cần
  - Cache ward data đã load
  
- [ ] **5.2.3** Add loading states
  - Skeleton screens
  - Progress indicators

**Ước tính thời gian:** 2-3 giờ

---

## 📊 TỔNG KẾT

### **Timeline Ước Tính:**
```
Phase 1: Data Crawling          → 2-3 giờ
Phase 2: Type Definitions       → 0.5 giờ
Phase 3: Map Components         → 6-8 giờ
Phase 4: Styling & UX           → 2-3 giờ
Phase 5: Testing & Optimization → 4-6 giờ
─────────────────────────────────────────
TOTAL:                          → 14.5-20.5 giờ
```

### **Thứ Tự Ưu Tiên:**
1. ✅ **Phase 1** - Crawl data (QUAN TRỌNG NHẤT)
2. ✅ **Phase 2** - Update types
3. ✅ **Phase 3.1** - Implement basic map
4. ✅ **Phase 3.2** - Add statistics panel
5. ✅ **Phase 4** - Styling
6. ✅ **Phase 3.3** - Add toggle
7. ✅ **Phase 5** - Testing & optimization

### **Dependencies:**
```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5
                         ↓
                    Phase 3.3
```

### **Deliverables:**
- ✅ Bản đồ 34 tỉnh mới với màu sắc phân biệt
- ✅ Click tỉnh → Zoom + Hiển thị đường biên giới xã/phường
- ✅ Panel thống kê với bảng danh sách xã/phường
- ✅ Tìm kiếm xã/phường
- ✅ Toggle giữa 63 tỉnh cũ ↔ 34 tỉnh mới
- ✅ Responsive design
- ✅ Smooth animations

---

## 🚀 NEXT STEPS

**Bắt đầu ngay:**
1. Tạo script crawl data (Phase 1.1)
2. Download tất cả GeoJSON files
3. Validate data
4. Implement map component

**Bạn muốn tôi bắt đầu với Phase nào?**
- [ ] Phase 1: Crawl data ngay
- [ ] Xem thêm chi tiết về một Phase cụ thể
- [ ] Điều chỉnh plan theo ý bạn
