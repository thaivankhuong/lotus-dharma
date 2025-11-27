import os
import json
import re
import time
import urllib.request
import urllib.parse
import ssl

# Bỏ qua verify SSL nếu cần (cho local dev)
ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

BASE_URL = "https://sapnhap.bando.com.vn"
# Đường dẫn tương đối từ thư mục scripts
DATA_DIR = os.path.join("..", "public", "data", "new-provinces")
DETAILS_DIR = os.path.join(DATA_DIR, "details")

def get_content(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(req, context=ctx) as response:
        return response.read().decode('utf-8')

def post_content(url, data):
    data = urllib.parse.urlencode(data).encode()
    req = urllib.request.Request(url, data=data, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(req, context=ctx) as response:
        return response.read().decode('utf-8')

def main():
    print("Dang lay du lieu tu trang chu...")
    try:
        html = get_content(BASE_URL)
    except Exception as e:
        print(f"Loi ket noi trang chu: {e}")
        return

    # Hardcoded IDs from browser inspection
    # IDs retrieved: 1,7,8,9,13,14,15,10,11,3,12,2,4,5,6,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34
    province_ids = [1,7,8,9,13,14,15,10,11,3,12,2,4,5,6,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34]
    
    print(f"Su dung danh sach ID cung: {len(province_ids)} tinh.")

    if not os.path.exists(DATA_DIR):
        os.makedirs(DATA_DIR)
    if not os.path.exists(DETAILS_DIR):
        os.makedirs(DETAILS_DIR)

    # Crawl chi tiet
    for mahc in province_ids:
        mahc_str = str(mahc)
        file_path = os.path.join(DETAILS_DIR, f"{mahc_str}.json")
        
        if os.path.exists(file_path):
             print(f"Da co du lieu cho ID {mahc_str}. Bo qua.")
             continue

        print(f"Dang tai du lieu cho ID: {mahc_str}...")
        
        try:
            detail_json_str = post_content(f"{BASE_URL}/ptracuu", {'id': mahc_str})
            detail_data = json.loads(detail_json_str)
            
            with open(file_path, "w", encoding="utf-8") as f:
                json.dump(detail_data, f, ensure_ascii=False, indent=2)
                
            print(f"  -> Xong.")
        except Exception as e:
            print(f"  -> Loi tai ID {mahc_str}: {e}")
        
        time.sleep(1) # Sleep 1s de lich su

if __name__ == "__main__":
    main()
