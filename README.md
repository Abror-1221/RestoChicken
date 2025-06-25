+++ RestoAW - Sistem Pemesanan dan Pembayaran
RestoAW adalah aplikasi web untuk pemesanan produk makanan/minuman secara online dengan fitur pembayaran terintegrasi menggunakan Midtrans Snap.

===================== Simulasi =====================

Link: http://resto-chicken.runasp.net/

Untuk simulasi Snap Midtrans,
pilih kartu kredit dengan nomor kartu:

Card number	4811 1111 1111 1114
Expiry date	12/30 (atau bebas, asal valid future)
CVV		123

====================================================

++ Alur Pengguna
- Halaman Utama
Pengguna dapat melihat daftar produk, menambah/mengurangi item ke keranjang, dan melanjutkan ke checkout.

- Product List Page
Pengguna memilih item untuk memasukan ke keranjang

- Checkout Page
Pengguna mengisi informasi diri dan melihat item yang dipesan.

- Pembayaran

Sistem menyimpan transaksi ke database.

Token pembayaran dari Midtrans diambil.

Midtrans Snap terbuka untuk proses pembayaran.

Setelah selesai, pengguna diarahkan ke halaman status transaksi.

- Cek Status Transaksi
Pengguna bisa cek status dengan memasukkan email di halaman utama.


++ Alur Kode

Validasi form, kalkulasi harga, dan logika interaksi dilakukan di frontend.

Backend menggunakan ASP.NET Core MVC dengan:

Pattern Repository + Service

ADO.NET untuk akses database

Pembayaran memakai Midtrans Snap (Sandbox) dengan tokenisasi dan webhook.


++ Struktur Database
Table_Users
Menyimpan data pembeli

Table_CartItems
Data keranjang (1 per transaksi)

Table_Carts_Products
Rincian item pada keranjang

Table_Products
Data list item

Table_Transactions
Total transaksi dan status

Table_PaymentNotif
Informasi dan status dari Midtrans (dummy/manual/webhook)
