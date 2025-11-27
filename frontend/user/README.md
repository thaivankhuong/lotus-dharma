# 🗺️ Bản Đồ Chùa Việt Nam

> Ứng dụng bản đồ tương tác Việt Nam với thiết kế Modern Zen Buddhist

![Next.js](https://img.shields.io/badge/Next.js-16.0.2-black)
![React](https://img.shields.io/badge/React-19.2.0-blue)
![TypeScript](https://img.shields.io/badge/TypeScript-5-blue)
![TailwindCSS](https://img.shields.io/badge/TailwindCSS-4-38bdf8)

## ✨ Tính Năng

- 🗺️ **Bản đồ SVG tương tác** - 63 tỉnh thành Việt Nam
- 🎨 **Modern Zen Design** - Phong cách Phật giáo tối giản, thanh lịch
- 🖱️ **Hover Effects** - Màu sắc thay đổi mượt mà khi di chuột
- 💬 **Smart Tooltip** - Hiển thị thông tin với delay 80ms
- 📊 **Side Panel** - Xem chi tiết tỉnh và danh sách xã/phường
- ⚡ **Smooth Animations** - Transitions 200ms cho mọi tương tác
- 📱 **Responsive Design** - Tối ưu cho mọi kích thước màn hình

## 🎨 Color Palette

```css
Zen Gold:   #d7b46a  /* Vàng đất - hover tỉnh */
Zen Earth:  #8b7355  /* Nâu đất */
Zen Stone:  #e8e8e8  /* Xám nhạt - màu tỉnh */
Zen Sky:    #89c4e1  /* Xanh nhạt - hover xã */
Zen Cream:  #f5f1e8  /* Kem nhạt */
```

## 🚀 Quick Start

### Prerequisites

- Node.js 20.9.0 hoặc cao hơn
- npm 11.6.2 hoặc cao hơn

### Installation

```bash
# Clone repository
git clone <repository-url>

# Navigate to project
cd lotus-dharma/frontend/user

# Install dependencies
npm install

# Run development server
npm run dev
```

Mở trình duyệt và truy cập: **http://localhost:3000**

## 📁 Project Structure

```
├── app/
│   ├── components/
│   │   ├── VietnamMap.tsx      # Bản đồ SVG chính
│   │   ├── Tooltip.tsx         # Tooltip component
│   │   └── SidePanel.tsx       # Panel thông tin
│   ├── types/
│   │   └── map.ts              # TypeScript types
│   ├── utils/
│   │   └── dataLoader.ts       # Data loading utilities
│   ├── globals.css             # Modern Zen styles
│   └── page.tsx                # Homepage
├── public/
│   └── data/
│       ├── province.json       # Dữ liệu 63 tỉnh thành
│       ├── ward.json          # Dữ liệu xã/phường
│       └── vietnam-map.json   # SVG paths
└── VIETNAM_MAP_GUIDE.md       # Hướng dẫn chi tiết
```

## 🎯 Usage

### 1. Xem thông tin tỉnh
Di chuột qua bất kỳ tỉnh nào trên bản đồ để xem tên

### 2. Xem chi tiết
Click vào tỉnh để mở panel bên phải với:
- Tên đầy đủ tỉnh
- Mã tỉnh
- Danh sách xã/phường

### 3. Đóng panel
- Click nút ✕ trên panel
- Click vào vùng tối bên ngoài panel

## 📊 Data Sources

### Province Data
```json
{
  "11": {
    "code": "11",
    "name": "Hà Nội",
    "slug": "ha-noi",
    "type": "thanh-pho",
    "name_with_type": "Thành phố Hà Nội"
  }
}
```

### Ward Data
```json
{
  "267": {
    "code": "267",
    "name": "Minh Châu",
    "type": "xa",
    "parent_code": "11",
    "name_with_type": "Xã Minh Châu"
  }
}
```

## 🛠️ Tech Stack

- **Framework**: Next.js 16.0.2 (App Router)
- **UI Library**: React 19.2.0
- **Language**: TypeScript 5
- **Styling**: TailwindCSS 4
- **Graphics**: SVG (native)

## 📖 Documentation

Xem [VIETNAM_MAP_GUIDE.md](./VIETNAM_MAP_GUIDE.md) để biết thêm chi tiết về:
- Cấu trúc dữ liệu
- Customization options
- Troubleshooting
- Future enhancements

## 🎨 Design Philosophy

Ứng dụng được thiết kế theo phong cách **Modern Zen Buddhist**:

- ☯️ **Tối giản**: Giao diện sạch, không rườm rà
- 🌾 **Màu đất**: Sử dụng tông màu đất, vàng, nâu
- 🍃 **Mượt mà**: Transitions và animations nhẹ nhàng
- 🧘 **Cân bằng**: Layout hài hòa, dễ nhìn

## 🔧 Development

### Build for Production
```bash
npm run build
```

### Start Production Server
```bash
npm start
```

### Lint Code
```bash
npm run lint
```

## 📝 Notes

- SVG paths trong `vietnam-map.json` là simplified representations
- Để có độ chính xác cao hơn, cần sử dụng GeoJSON data thực tế
- Ứng dụng sử dụng client-side rendering cho interactive components

## 🌟 Future Roadmap

- [ ] Tìm kiếm tỉnh/xã
- [ ] Filter theo loại (thành phố/tỉnh)
- [ ] Zoom & Pan functionality
- [ ] Markers cho các chùa
- [ ] Export data (PDF/Excel)
- [ ] Dark mode
- [ ] Mobile app version

## 📄 License

© 2025 Bản Đồ Chùa Việt Nam

---

**Made with ❤️ and ☯️ Zen**
