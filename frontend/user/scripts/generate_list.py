import os
import json
import glob

# Đường dẫn tương đối từ thư mục scripts
DETAILS_DIR = os.path.join("..", "public", "data", "new-provinces", "details")
OUTPUT_FILE = os.path.join("..", "public", "data", "new-provinces", "provinces_list.json")

def main():
    provinces = []
    print("Dang quet du lieu chi tiet de tao danh sach tinh...")
    
    files = glob.glob(os.path.join(DETAILS_DIR, "*.json"))
    for filepath in files:
        try:
            with open(filepath, "r", encoding="utf-8") as f:
                data = json.load(f)
                if data and len(data) > 0:
                    # Lấy thông tin từ item đầu tiên
                    first_item = data[0]
                    # Filename là ID dùng để gọi API
                    file_id = os.path.splitext(os.path.basename(filepath))[0]
                    
                    provinces.append({
                        "mahc": file_id, 
                        "tentinh": first_item.get("tentinh")
                    })
        except Exception as e:
            print(f"Loi doc file {filepath}: {e}")

    # Sort by ID (convert to int for correct sorting)
    provinces.sort(key=lambda x: int(x["mahc"]) if x["mahc"].isdigit() else x["mahc"])

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(provinces, f, ensure_ascii=False, indent=2)

    print(f"Da tao danh sach {len(provinces)} tinh vao {OUTPUT_FILE}.")

if __name__ == "__main__":
    main()
