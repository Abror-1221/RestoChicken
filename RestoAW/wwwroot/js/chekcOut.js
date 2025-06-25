window.addEventListener('DOMContentLoaded', () => {
    const cartData = JSON.parse(localStorage.getItem('cartItems')) || {};
    const container = document.querySelector('.list-item');
    const totalElement = document.querySelector('.text-end span.fw-bold');

    container.innerHTML = '';
    let totalPayment = 0;

    // === START: Loop Content Product List in Cart ===
    for (const [name, item] of Object.entries(cartData)) {
        const subtotal = item.price * item.quantity;
        totalPayment += subtotal;

        const element = document.createElement('div');
        element.className = 'd-flex justify-content-between align-items-start checkout-item pt-4';
        element.setAttribute('data-name', name);
        element.innerHTML = `
            <img src="${item.image}" alt="${name}" class="me-3 align-self-center" style="width: 80px; height: 80px; object-fit: cover; border-radius: 10px;">
            <div class="flex-grow-1">
                <h6 class="mb-1">${name}</h6>
                <p class="mb-1">Price: Rp${item.price.toLocaleString()}</p>
                <p class="mb-1">Amount: <span class="item-quantity">${item.quantity}</span></p>
                <p class="fw-bold mb-2">Total: <span class="item-total">Rp${subtotal.toLocaleString()}</span></p>
                <input type="text" class="form-control form-control-sm note-input" placeholder="Note...">
            </div>
            <div class="ms-3 d-flex flex-column justify-content-between align-items-end">
                <button class="btn btn-outline-secondary btn-sm btn-plus mb-2" style="width: 32px; height: 32px;">
                    <i class="fas fa-plus"></i>
                </button>
                <button class="btn btn-outline-secondary btn-sm btn-minus" style="width: 32px; height: 32px;">
                    <i class="fas fa-minus"></i>
                </button>
            </div>
        `;
        container.appendChild(element);
    }

    totalElement.textContent = 'Rp' + totalPayment.toLocaleString();
    // === END: Loop Content Product List in Cart ===

    // === START: Validasi Form ===
    const form = document.getElementById("checkoutForm");
    form.addEventListener("submit", (e) => {
        e.preventDefault();

        const name = document.getElementById("name").value.trim();
        const email = document.getElementById("email").value.trim();
        const address = document.getElementById("address").value.trim();
        const phone = document.getElementById("phone").value.trim();

        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        const phonePattern = /^\+62[0-9]{9,13}$/;

        ["name", "email", "address", "phone"].forEach(id => {
            document.getElementById(`error-${id}`).textContent = "";
        });

        let valid = true;

        const cartDataUpdate = JSON.parse(localStorage.getItem('cartItems')) || {};

        if (name === "") {
            document.getElementById("error-name").textContent = "Name is required.";
            valid = false;
        }

        if (!emailPattern.test(email)) {
            document.getElementById("error-email").textContent = "Invalid email format.";
            valid = false;
        }

        if (address === "") {
            document.getElementById("error-address").textContent = "Address is required.";
            valid = false;
        }

        if (!phonePattern.test(phone)) {
            document.getElementById("error-phone").textContent = "Phone must start with +62 and contain only digits.";
            valid = false;
        }

        if (!valid) return;

        document.querySelectorAll('.checkout-item').forEach(itemEl => {
            const itemName = itemEl.getAttribute('data-name');
            const note = itemEl.querySelector('.note-input')?.value.trim() || "";

            if (cartDataUpdate[itemName]) {
                cartDataUpdate[itemName].note = note;
            }
        });

        Swal.fire({
            title: "Confirm Payment",
            text: "Are you sure you want to proceed?",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Yes, pay now"
        }).then(result => {
            if (!result.isConfirmed) return;

            const payload = {
                buyer: { name, email, address, phone },
                cartItems: cartDataUpdate
            };

            Swal.fire({
                title: 'Processing...',
                text: 'Saving your transaction...',
                allowOutsideClick: false,
                didOpen: () => Swal.showLoading()
            });

            // Send to DB
            fetch('/Product/TransactionsPay', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            })
            .then(res => res.json())
            .then(data => {
                if (data.status !== "success") {
                    Swal.fire("Oops!", "Failed to save transaction to server.", "error");
                    return;
                }

                fetch('/api/payment/token', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                })
                .then(res => res.json())
                .then(data => {
                    Swal.close();

                    if (!data.token) {
                        Swal.fire("Error", "Failed to get payment token.", "error");
                        return;
                    }

                    snap.pay(data.token, {
                        onSuccess: async function (result) {
                            await new Promise(resolve => setTimeout(resolve, 400));
                            sendToServer(result);
                        },
                        onPending: async function (result) {
                            await new Promise(resolve => setTimeout(resolve, 400));
                            sendToServer(result);
                        },
                        onError: async function (result) {
                            await new Promise(resolve => setTimeout(resolve, 400));
                            sendToServer(result);
                        }
                    });
                });
            });

            async function sendToServer(result) {
                const status = result.transaction_status || "failed";
                const orderId = result.order_id;
                const rawJson = JSON.stringify(result);
                const receivedAt = new Date().toISOString();
                const email = document.getElementById("email").value.trim();

                Swal.fire({
                    title: 'Updating Transaction...',
                    text: 'Please wait while we update the payment status.',
                    allowOutsideClick: false,
                    didOpen: () => Swal.showLoading()
                });

                try {
                    const res = await fetch('/api/payment/update-status', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ orderId, status, rawJson, receivedAt })
                    });

                    if (!res.ok) throw new Error("Update status failed");

                    Swal.close();
                    window.location.href = `/Product/Transactions?email=${encodeURIComponent(email)}&status=${status}`;
                } catch (err) {
                    Swal.fire("Error", "Failed to update transaction status.", "error");
                }
            }
        });
    });
    // === END: Validasi Form ===
});
