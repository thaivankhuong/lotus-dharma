# Hướng Dẫn Sử Dụng Bản Đồ Việt Nam

## 🎉 Tổng Quan

Ứng dụng bản đồ Việt Nam tương tác đã được triển khai thành công với các tính năng:

### ✅ Đã Hoàn Thành (Phase 1)

1. **Hiển thị 64 tỉnh thành cũ** từ file `geoBoundaries-VNM-ADM1.geojson`
2. **Hover tỉnh → Highlight viền** - Di chuột qua tỉnh sẽ highlight màu xanh nhạt
3. **Click tỉnh → Zoom vào tỉnh** - Bản đồ tự động zoom vào tỉnh được chọn
4. **Hiển thị đường biên giới huyện** - Sau khi click, các đường biên giới huyện/quận sẽ hiển thị màu đỏ
5. **Panel thông tin** - Hiển thị tên tỉnh đã chọn
6. **Reset view** - Nút reset để quay lại view toàn quốc

### 🔄 Đang Phát Triển (Phase 2)

1. **Merge 64 tỉnh → 34 tỉnh** theo sát nhập 2025
2. **Hiển thị phường/xã** thay vì huyện cho 34 tỉnh mới
3. **Toggle giữa 2 chế độ** xem (63 tỉnh cũ / 34 tỉnh mới)

## 📂 Cấu Trúc Dữ Liệu

```
public/documents/data Province Vietnam/
├── geoBoundaries-VNM-ADM1.geojson  # 64 tỉnh thành cũ (GeoJSON)
├── geoBoundaries-VNM-ADM2.geojson  # 708 huyện/quận (GeoJSON)
├── province.json                    # 34 tỉnh mới sau sát nhập
└── ward.json                        # 33,000+ phường/xã mới
```

## 🛠️ Công Nghệ Sử Dụng

- **React + Next.js 15** - Framework chính
- **Leaflet** - Thư viện bản đồ
- **Turf.js** - Xử lý geometry (point-in-polygon detection)
- **TypeScript** - Type safety
- **Tailwind CSS** - Styling

## 🎯 Cách Sử Dụng

### Truy Cập Bản Đồ

Mở trình duyệt và truy cập: **http://localhost:3000/map**

### Tương Tác

1. **Xem toàn bộ Việt Nam**: Bản đồ mặc định hiển thị 64 tỉnh thành
2. **Hover tỉnh**: Di chuột qua tỉnh để xem tên và highlight
3. **Click tỉnh**: Click vào tỉnh để:
   - Zoom vào tỉnh đó
   - Hiển thị đường biên giới các huyện/quận
   - Xem tên tỉnh trong panel bên phải
4. **Reset**: Click nút "Reset View" để quay lại view toàn quốc

## 🎨 Giao Diện

### Panel Điều Khiển (Góc Phải Trên)
- **Chế độ xem**: Toggle giữa 63 tỉnh cũ / 34 tỉnh mới (đang phát triển)
- **Thông tin tỉnh**: Hiển thị tên tỉnh đã chọn
- **Reset View**: Quay lại view toàn quốc
- **Thống kê**: Số lượng tỉnh thành và phường/xã

### Chú Thích (Góc Trái Dưới)
- **Màu xám**: Tỉnh thành bình thường
- **Màu xanh**: Tỉnh đã chọn
- **Viền đỏ**: Đường biên giới huyện/quận

## 🔧 Kỹ Thuật Triển Khai

### 1. Load Dữ Liệu
```typescript
// Load tất cả dữ liệu song song
const [provinces63, districts, provinces34, wards] = await Promise.all([
  fetch('/documents/data Province Vietnam/geoBoundaries-VNM-ADM1.geojson'),
  fetch('/documents/data Province Vietnam/geoBoundaries-VNM-ADM2.geojson'),
  fetch('/documents/data Province Vietnam/province.json'),
  fetch('/documents/data Province Vietnam/ward.json'),
]);
```

### 2. Render Tỉnh với Leaflet
```typescript
const provincesLayer = L.geoJSON(mapData.provinces63, {
  style: provinceStyle,
  onEachFeature: (feature, layer) => {
    // Hover, click handlers
    // Tooltips
  },
});
```

### 3. Filter Huyện Theo Tỉnh
Sử dụng **Turf.js** để kiểm tra huyện nào nằm trong tỉnh:

```typescript
const provincePoly = turf.feature(provinceFeature.geometry);

const provinceDistricts = mapData.districts.features.filter((districtFeature) => {
  const districtCentroid = turf.centroid(districtFeature.geometry);
  return turf.booleanPointInPolygon(districtCentroid, provincePoly);
});
```

**Lý do**: Dữ liệu ADM2 không có thông tin về tỉnh mẹ, nên phải dùng geometry intersection.

## 📝 Kế Hoạch Tiếp Theo

### Phase 2: Sát Nhập Tỉnh (34 Tỉnh Mới)

#### Bước 1: Tạo Mapping Table
Cần nghiên cứu quyết định sát nhập chính thức để tạo mapping:

```json
{
  "Hà Nội": {
    "oldProvinces": ["Hà Nội", "Hà Tây"],
    "newCode": "11"
  },
  "Hải Phòng": {
    "oldProvinces": ["Hải Phòng", "Hải Dương"],
    "newCode": "14"
  }
  // ... 32 tỉnh khác
}
```

#### Bước 2: Merge Geometries
Sử dụng Turf.js để merge:

```typescript
import * as turf from '@turf/turf';

function mergeProvinces(oldProvinces: Feature[]): Feature {
  let merged = oldProvinces[0];
  for (let i = 1; i < oldProvinces.length; i++) {
    merged = turf.union(merged, oldProvinces[i]);
  }
  return turf.simplify(merged, { tolerance: 0.01 });
}
```

#### Bước 3: Hiển thị Phường/Xã
Khi click tỉnh mới, hiển thị phường/xã từ `ward.json`:

```typescript
const wards = Object.values(mapData.wards).filter(
  (ward) => ward.parent_code === provinceCode
);

// Render ward names as markers or labels
```

## 🐛 Xử Lý Vấn Đề

### Vấn Đề 1: Dữ liệu không load
**Nguyên nhân**: File không nằm trong thư mục `public`
**Giải pháp**: Di chuyển thư mục `documents` vào `public/`

### Vấn Đề 2: Không filter được huyện theo tỉnh
**Nguyên nhân**: ADM2 không có thông tin tỉnh mẹ
**Giải pháp**: Dùng Turf.js `booleanPointInPolygon` để check geometry

### Vấn Đề 3: Performance chậm khi render nhiều huyện
**Giải pháp**: 
- Simplify geometries với Turf.js
- Chỉ render khi zoom level đủ lớn
- Use React.memo cho components

## 📊 Thống Kê Dữ Liệu

- **Tỉnh thành cũ**: 64 features
- **Huyện/Quận**: 708 features  
- **Tỉnh thành mới**: 34 provinces
- **Phường/Xã mới**: 33,212 wards

## 🎓 Tài Liệu Tham Khảo

- [Leaflet Documentation](https://leafletjs.com/)
- [Turf.js Documentation](https://turfjs.org/)
- [GeoJSON Specification](https://geojson.org/)
- [React-Leaflet](https://react-leaflet.js.org/)

## 📞 Hỗ Trợ

Nếu gặp vấn đề, kiểm tra:
1. Console log trong browser (F12)
2. Network tab để xem file có load được không
3. File structure trong `public/documents/`

---

**Phiên bản**: 1.0.0 (Phase 1 Complete)
**Ngày cập nhật**: 2025-01-22
