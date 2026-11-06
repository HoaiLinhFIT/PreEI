# Hướng dẫn chạy PreEI Version 1 trên GitHub

## 1. Chuẩn bị dữ liệu

Tải file Connect **đã gán profit** lên repository tại `datasets/connect.txt`. File dữ liệu này không có sẵn trong gói mã nguồn. Nếu muốn dùng dữ liệu khác, tải lên thư mục `datasets/` và chọn đường dẫn của file khi chạy.

## 2. Chạy GitHub Actions

1. Trong repository, mở **Actions**.
2. Ở bên trái, chọn **Run PreEI Version 1**.
3. Nhấn **Run workflow** và chọn nhánh mặc định (`main`, nếu repository dùng nhánh này).
4. Giữ nguyên các giá trị mặc định nếu chạy Connect:
   - Dataset: `datasets/connect.txt`
   - Lower threshold: `2.5`
   - Upper threshold: `2.8`
   - Verify reference: bật
5. Nhấn **Run workflow** để bắt đầu.

Chương trình tự chia dữ liệu thành **80% ban đầu + 10 batch, mỗi batch 2%**, sau đó khai phá đủ 11 giai đoạn.

## 3. Xem kiểm chứng và tải kết quả

Mở lần chạy trong **Actions**:

- Job **mine** sẽ có bước **Verify 11 stages against matching reference**.
- Nếu có tham chiếu phù hợp, bước kiểm chứng sẽ trả về **PASS** hoặc báo lỗi khi kết quả không khớp.
- Nếu không có tham chiếu phù hợp, trạng thái là **SKIP**; điều đó không có nghĩa là đã kiểm chứng thành công.
- Trong trang **Summary**, tìm **Artifacts** và tải gói `preei-version-1-run-...` để lấy CSV, TXT, `run_metadata.json` và `validation_status.txt`.

## 4. Chạy dataset khác

Ở **Run workflow**, thay `dataset` bằng đường dẫn tương đối tới file đã tải lên (ví dụ `datasets/retail.txt`), rồi nhập lower/upper phù hợp. Phép chia 80% + 10 × 2% áp dụng cho tất cả dataset. Muốn kiểm chứng tự động thêm dataset, khai báo một profile phù hợp trong `validation/reference_profiles.json` và cung cấp file CSV tham chiếu của dataset đó.

## 5. Phân biệt các số đo

GitHub Actions dùng Windows runner + .NET 10 (Console), còn ứng dụng WinForms dùng .NET 6. Runtime và peak memory có thể khác trên các máy khác nhau. Hãy sử dụng GitHub Actions chủ yếu để kiểm tra số lượng itemsets và quyết định incremental update/full re-scan; không lấy số đo GitHub thay trực tiếp số liệu benchmark từ máy thí nghiệm khác.

**Lưu ý:** Đổi tên sang PreEI Version 1 không làm thay đổi thuật toán, ngưỡng, điều kiện re-scan, `result_EIs` hoặc cách đo runtime/memory.
