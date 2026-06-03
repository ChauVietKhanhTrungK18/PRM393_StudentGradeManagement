# AI Feature — Tài liệu API & Hướng dẫn Test

## Base URL

| Môi trường | URL |
|-----------|-----|
| HTTPS (dev) | `https://localhost:7086` |
| HTTP (dev) | `http://localhost:5062` |
| Swagger UI | `https://localhost:7086/swagger` |

---

## Danh sách API cho Frontend

### 1. AI Chat — Hỏi đáp tự do về lớp học

```
POST /api/ai/chat
Content-Type: application/json
```

**Request:**
```json
{
  "subjectCode": "PRN221",
  "className": "NET1710",
  "question": "Lớp này có bao nhiêu bạn điểm Group Project lớn hơn 8?"
}
```

**Response:**
```json
{
  "answer": "Có 11 sinh viên có điểm Group Project lớn hơn 8.",
  "relatedData": {
    "totalStudents": 35,
    "passCount": 0,
    "failCount": 0,
    "averageScore": 0
  }
}
```

> `passCount/failCount/averageScore` = 0 khi chưa có đủ điểm tất cả thành phần (bình thường).

---

### 2. Thống kê + Nhận xét AI

```
GET /api/ai/statistics/{subjectCode}/{className}
```

**Ví dụ:** `GET /api/ai/statistics/PRN221/NET1710`

**Response:**
```json
{
  "subjectCode": "PRN221",
  "className": "NET1710",
  "totalStudents": 35,
  "passCount": 0,
  "failCount": 0,
  "passRate": 0.0,
  "averageScore": 0.0,
  "componentStats": [
    {
      "name": "Group Project",
      "average": 7.81,
      "zeroCount": 0,
      "emptyCount": 0
    },
    {
      "name": "Final Exam",
      "average": 0.0,
      "zeroCount": 35,
      "emptyCount": 0
    }
  ],
  "gradeDistribution": {
    "A": 0, "B+": 0, "B": 0, "C+": 0, "C": 0, "D+": 0, "D": 0, "F": 0
  },
  "aiInsights": [
    "Lớp có điểm Group Project trung bình 7.81, phần lớn sinh viên đạt từ 7 trở lên.",
    "Chưa có điểm Final Exam và Practical Exam — chưa thể đánh giá kết quả tổng kết."
  ]
}
```

> `passCount/gradeDistribution` đều = 0 khi thiếu các thành phần chưa thi. `aiInsights` luôn có nội dung (do AI tạo).

---

### 3. Phát hiện điểm bất thường

```
GET /api/ai/anomalies/{subjectCode}/{className}
```

**Ví dụ:** `GET /api/ai/anomalies/PRN221/NET1710`

**Response:**
```json
{
  "anomalies": [
    {
      "rollNumber": "SE160920",
      "fullName": "Lê Ngô Hiệp Quốc",
      "type": "OUTLIER_LOW",
      "description": "Group Project = 6.4 thấp hơn nhiều trung bình lớp 7.8 (z=-2.9).",
      "severity": "Warning"
    }
  ],
  "summary": "Phát hiện 2 trường hợp bất thường cần xem lại."
}
```

**Các giá trị `type`:**

| type | Ý nghĩa | severity |
|------|---------|---------|
| `POSSIBLE_INPUT_ERROR` | Điểm âm hoặc vượt MaxMark | `Error` |
| `HIGH_COMPONENT_LOW_TOTAL` | Thành phần cao nhưng tổng thấp | `Warning` |
| `INCONSISTENT_PATTERN` | Điểm các thành phần không đồng đều (σ > 2.5) | `Warning` |
| `OUTLIER_HIGH` | Điểm vượt xa TB lớp (z > 2.0) | `Info` |
| `OUTLIER_LOW` | Điểm thấp hơn nhiều TB lớp (z < −2.0) | `Warning` |

> Tính năng này **không dùng AI** — toàn bộ C# tính. Trả về ngay, không phụ thuộc LM Studio.

---

### 4. Gợi ý comment cho sinh viên

```
POST /api/ai/suggest-comments/{subjectCode}/{className}
```

**Ví dụ:** `POST /api/ai/suggest-comments/PRN221/NET1710`

**Không cần request body.**

**Response:**
```json
{
  "suggestions": [
    {
      "rollNumber": "SE161263",
      "fullName": "Nguyễn Mạnh Duy",
      "suggestedComment": "Kết quả xuất sắc, tiếp tục duy trì phong độ học tập.",
      "confidence": "HIGH"
    },
    {
      "rollNumber": "SE160920",
      "fullName": "Lê Ngô Hiệp Quốc",
      "suggestedComment": "Cần cố gắng hơn ở các bài kiểm tra.",
      "confidence": "LOW"
    }
  ]
}
```

**Giá trị `confidence`:** `HIGH` (tổng ≥ 7.0) · `MEDIUM` (5.0–6.9) · `LOW` (< 5.0 hoặc chưa đủ điểm)

> Số phần tử `suggestions` = tổng số SV trong lớp. SV bị AI bỏ sót được tự động fill `"Cần xem xét thêm."`.

---

## Lưu ý cho Frontend

| Trường hợp | Xử lý |
|-----------|-------|
| `passCount = 0`, `averageScore = 0` | Bình thường — lớp chưa đủ điểm tất cả thành phần |
| `anomalies = []` | Không có bất thường, không phải lỗi |
| `aiInsights` có 3–5 mục | Luôn có nội dung nếu đã nhập điểm |
| `answer` chứa "AI chưa được cấu hình" | Backend chưa set API key LM Studio |
| Response chậm (10–30s) | Bình thường — model local cần thời gian xử lý |

---

## Cấu hình backend (`appsettings.json`)

```json
"AI": {
  "BaseUrl": "http://127.0.0.1:1234",
  "ApiKey": "lm-studio",
  "Model": "qwen2.5-7b-instruct",
  "MaxTokens": 1024,
  "TimeoutSeconds": 60
}
```

---

---

# Hướng dẫn Test Luồng AI

## Tổng quan luồng

```
[1] Khởi động LM Studio (context ≥ 8192)
        ↓
[2] Chạy API (F5 trong Visual Studio)
        ↓
[3] Import file .fg vào DB
        ↓
[4] Xác nhận data trong DB
        ↓
[5] Test 4 tính năng AI theo thứ tự
```

---

## Bước 1 — Khởi động LM Studio

1. Mở **LM Studio** → tab **Developer** → **Local Server**
2. Chọn model **qwen2.5-7b-instruct** → tab **Load** → đặt **Context Length = 8192** → nhấn **Load**
3. Nhấn **Start Server** → xác nhận `Status: Running` tại `http://127.0.0.1:1234`

**Kiểm tra nhanh:**
```bash
curl -X POST http://127.0.0.1:1234/v1/messages \
  -H "Content-Type: application/json" \
  -H "x-api-key: lm-studio" \
  -H "anthropic-version: 2023-06-01" \
  -d "{\"model\":\"qwen2.5-7b-instruct\",\"max_tokens\":50,\"messages\":[{\"role\":\"user\",\"content\":\"Xin chao\"}]}"
```
Kết quả có `content[0].text` → LM Studio hoạt động.

---

## Bước 2 — Chạy API

Nhấn **F5** trong Visual Studio hoặc chọn profile **https**.

Mở `https://localhost:7086/swagger` để xác nhận API đang chạy.

---

## Bước 3 — Import file .fg

Trong Swagger: `POST /api/fg/import` → **Try it out** → chọn file `.fg` → **Execute**

```json
// Kết quả thành công:
{
  "subjectClassCount": 18,
  "studentCount": 630,
  "componentCount": 162,
  "markCount": 5670
}
```

---

## Bước 4 — Xác nhận data trong DB

```bash
# Lấy danh sách lớp
GET /api/subjects

# Xem điểm một lớp cụ thể
GET /api/subjects/PRN221/NET1710
```

Kiểm tra `marks` có giá trị thực (không toàn null) → data đã import đúng.

---

## Bước 5 — Test 4 tính năng AI

> Dùng `subjectCode` và `className` lấy từ Bước 4.

---

### Test 1 — AI Chat

**Swagger:** `POST /api/ai/chat`

```json
{
  "subjectCode": "PRN221",
  "className": "NET1710",
  "question": "Lớp này có bao nhiêu bạn điểm Group Project lớn hơn 8?"
}
```

**Kết quả đúng:** `answer` nêu đúng số lượng (11 người), khớp với data trong FG tool.

**Các câu hỏi nên test thêm:**
```
"Sinh viên nào có điểm Group Project cao nhất?"
"Điểm trung bình của thành phần Assignment 1 là bao nhiêu?"
"Thành phần nào chưa có điểm?"
"Có bao nhiêu sinh viên điểm Assignment 3 nhỏ hơn 7?"
"Sinh viên SE161263 có điểm Group Project là bao nhiêu?"
```

---

### Test 2 — Thống kê + AI Insights

**Swagger:** `GET /api/ai/statistics/PRN221/NET1710`

**Kiểm tra:**
- `componentStats` liệt kê đúng tên thành phần
- `componentStats` cho Final Exam: `average = 0`, `zeroCount = 35` (chưa thi — đúng)
- `componentStats` cho Group Project: `average ≈ 7.81`
- `aiInsights` có 3–5 câu tiếng Việt, phản ánh đúng tình trạng lớp
- `passCount = 0` — đúng vì Final Exam chưa có điểm

---

### Test 3 — Phát hiện bất thường

**Swagger:** `GET /api/ai/anomalies/PRN221/NET1710`

**Kết quả đúng cho lớp này:**
- Không có `POSSIBLE_INPUT_ERROR` (điểm hợp lệ)
- Không có `INCONSISTENT_PATTERN` (thành phần chưa thi đã được loại)
- Có thể có `OUTLIER_LOW` cho SE160920 (6.4) và SE161614 (6.3)
- `summary` mô tả đúng số lượng

**Nếu thấy nhiều `POSSIBLE_INPUT_ERROR` cho điểm bình thường:** → App chưa restart sau khi fix.

---

### Test 4 — Gợi ý Comment

**Swagger:** `POST /api/ai/suggest-comments/PRN221/NET1710`

**Kiểm tra:**
- Số phần tử `suggestions` = 35 (đúng bằng tổng SV)
- SE161263 (8.5 — cao nhất) nhận `confidence: "LOW"` (vì chưa có tổng kết do thiếu Final Exam)
- Nội dung comment tiếng Việt, ngắn gọn (≤ 15 từ)
- SV bị AI bỏ sót sẽ có comment `"Cần xem xét thêm."`

---

## Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân | Cách xử lý |
|-----|------------|------------|
| `404 Not Found` | Sai `subjectCode`/`className` | Gọi `GET /api/subjects` lấy đúng tên |
| `"Yêu cầu AI hết thời gian"` | LM Studio chưa load model | Kiểm tra LM Studio, tăng `TimeoutSeconds: 120` |
| `"Lỗi Claude API (500): n_keep >= n_ctx"` | Context 4096 quá nhỏ | Tăng Context Length lên **8192** trong LM Studio |
| `"Lỗi Claude API (400)"` | Sai tên model | Sửa `AI:Model` khớp tên trong LM Studio |
| `answer` sai data | Model đọc nhầm | Restart app để nhận code fix mới nhất |
| Nhiều `POSSIBLE_INPUT_ERROR` | App chưa restart sau fix | Restart Visual Studio app |
| `passCount = 0` mọi lúc | Chưa nhập đủ điểm tất cả thành phần | Đây là hành vi đúng — không phải lỗi |
