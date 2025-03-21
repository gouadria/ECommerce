(function ($) {
    "use strict";
    
    // Dropdown on mouse hover
    $(document).ready(function () {
        function toggleNavbarMethod() {
            if ($(window).width() > 992) {
                $('.navbar .dropdown').on('mouseover', function () {
                    $('.dropdown-toggle', this).trigger('click');
                }).on('mouseout', function () {
                    $('.dropdown-toggle', this).trigger('click').blur();
                });
            } else {
                $('.navbar .dropdown').off('mouseover').off('mouseout');
            }
        }
        toggleNavbarMethod();
        $(window).resize(toggleNavbarMethod);
    });
    
    
    // Back to top button
    $(window).scroll(function () {
        if ($(this).scrollTop() > 100) {
            $('.back-to-top').fadeIn('slow');
        } else {
            $('.back-to-top').fadeOut('slow');
        }
    });
    $('.back-to-top').click(function () {
        $('html, body').animate({scrollTop: 0}, 1500, 'easeInOutExpo');
        return false;
    });


    // Vendor carousel
    $('.vendor-carousel').owlCarousel({
        loop: true,
        margin: 29,
        nav: false,
        autoplay: true,
        smartSpeed: 1000,
        responsive: {
            0:{
                items:2
            },
            576:{
                items:3
            },
            768:{
                items:4
            },
            992:{
                items:5
            },
            1200:{
                items:6
            }
        }
    });


    // Related carousel
    $('.related-carousel').owlCarousel({
        loop: true,
        margin: 29,
        nav: false,
        autoplay: true,
        smartSpeed: 1000,
        responsive: {
            0:{
                items:1
            },
            576:{
                items:2
            },
            768:{
                items:3
            },
            992:{
                items:4
            }
        }
    });


    // Product Quantity
    $('.quantity button').on('click', function () {
        var button = $(this);
        var oldValue = button.parent().parent().find('input').val();
        if (button.hasClass('btn-plus')) {
            var newVal = parseFloat(oldValue) + 1;
        } else {
            if (oldValue > 0) {
                var newVal = parseFloat(oldValue) - 1;
            } else {
                newVal = 0;
            }
        }
        button.parent().parent().find('input').val(newVal);
    });
    
})(jQuery);
function addToCart(productId, productName, productPrice) {
    console.log(`Product ID: ${productId}`);
    const quantityInput = document.querySelector(`#quantity-${productId}`);

    if (!quantityInput) {
        console.error(`Le champ de quantité pour le produit ${productId} n'a pas été trouvé.`);
        return;
    }

    const quantity = parseInt(quantityInput.value) || 1;
    console.log(`Quantité récupérée : ${quantity}`);

    let cart = JSON.parse(localStorage.getItem('cart')) || [];

    const existingProduct = cart.find(item => item.id === productId);
    if (existingProduct) {
        existingProduct.quantity += quantity;
    } else {
        cart.push({ id: productId, name: productName, price: productPrice, quantity: quantity });
    }

    localStorage.setItem('cart', JSON.stringify(cart));
    alert(` ${productName} a été ajouté au panier.`);
    updateCartDisplay();
    updateCartCount();
}

function addToCart1(productId, productName, productPrice, quantity = 1) {
    // Récupérer le panier depuis le localStorage ou initialiser un panier vide
    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    if(quantity<1){
         quantity=1
    }

    // Vérifier si le produit existe déjà dans le panier
    const existingProduct = cart.find(item => item.id === productId);

    if (existingProduct) {
        // Si le produit existe, on augmente simplement sa quantité
        existingProduct.quantity += 1;
    } else {
        // Sinon, on ajoute un nouvel objet produit dans le panier
        cart.push({
            id: productId,
            name: productName,
            price: productPrice,
            quantity: quantity,
        });
    }

    // Mettre à jour le panier dans le localStorage
    localStorage.setItem('cart', JSON.stringify(cart));

    // Afficher une alerte pour confirmer l'ajout
    alert(`${productName} a été ajouté au panier.`);

    // Mettre à jour l'affichage du panier (si nécessaire)
    updateCartDisplay();
    updateCartCount();
}

// Met à jour l'affichage du panier et le total
// Met à jour l'affichage du panier et le total
function updateCartDisplay() {
    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    const cartTable = document.getElementById('cartTableBody');
    let subtotal = 0;
    const shippingCost = 10;
    const totalElement = document.getElementById('total');
    const amountInput = document.getElementById('amount-input'); // Champ caché

    if (cartTable) {
        cartTable.innerHTML = ''; // Réinitialiser le tableau du panier

        // Parcourir les produits du panier
        cart.forEach(product => {
            const totalProduct = product.price * product.quantity;
            subtotal += totalProduct;

            const productRow = document.createElement('tr');
            productRow.innerHTML = `
                <td class="align-middle">
                    <img src="${product.imageUrl || ''}" alt="" style="width: 50px;"> ${product.name}
                </td>
                <td class="align-middle">$${product.price.toFixed(2)}</td>
                <td class="align-middle">
                    <div class="input-group quantity mx-auto" style="width: 100px;">
                        <div class="input-group-btn">
                            <button class="btn btn-sm btn-primary btn-minus" onclick="changeQuantity1(${product.id}, -1)">
                                <i class="fa fa-minus"></i>
                            </button>
                        </div>
                        <input type="text" class="form-control form-control-sm bg-secondary text-center" value=${product.quantity} disabled>
                        <div class="input-group-btn">
                            <button class="btn btn-sm btn-primary btn-plus" onclick="changeQuantity1(${product.id}, 1)">
                                <i class="fa fa-plus"></i>
                            </button>
                        </div>
                    </div>
                </td>
                <td class="align-middle" id="productTotal-${product.id}">$${totalProduct.toFixed(2)}</td>
                <td class="align-middle">
                    <button class="btn btn-sm btn-primary" onclick="removeFromCart(${product.id})">
                        <i class="fa fa-times"></i>
                    </button>
                </td>
            `;
            cartTable.appendChild(productRow);
        });
    }

    // Calculer et afficher le sous-total et le total
    const totalAmount = subtotal + shippingCost;

    const subtotalElement = document.getElementById('subtotal');
    if (subtotalElement) {
        subtotalElement.textContent = `$${subtotal.toFixed(2)}`;
    }

    if (totalElement) {
        totalElement.textContent = `$${totalAmount.toFixed(2)}`;
    }

    // Mettre à jour le champ amount-input avec le total
    if (amountInput) {
        amountInput.value = totalAmount;
    }

    // Enregistrer le total dans localStorage
    localStorage.setItem('totalAmount', totalAmount);
    
}

// Mettre à jour le panier au chargement de la page
document.addEventListener('DOMContentLoaded', () => {

    updateCartDisplay();  // Met à jour l'affichage du panier et le calcul du total
    updateCartCount();  // Met à jour le nombre d'articles dans le panier
   
});



// Fonction pour changer la quantité d'un produit
function changeQuantity1(productId, change) {
    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    const product = cart.find(item => item.id === productId);

    if (product) {
        product.quantity += change;

        // Empêcher les quantités négatives
        if (product.quantity < 1) {
            product.quantity = 1;
        }

        // Sauvegarder à nouveau le panier dans le localStorage
        localStorage.setItem('cart', JSON.stringify(cart));
        updateCartDisplay();  // Mettre à jour l'affichage du panier
    }
}

// Fonction pour supprimer un produit du panier
function removeFromCart(productId) {
    let cart = JSON.parse(localStorage.getItem('cart')) || [];

    // Filtrer le panier pour retirer le produit
    cart = cart.filter(item => item.id !== productId);

    // Sauvegarder à nouveau le panier dans le localStorage
    localStorage.setItem('cart', JSON.stringify(cart));

    // Mettre à jour l'affichage du panier et du total
    updateCartDisplay();
    updateCartCount();
}

// Fonction pour mettre à jour le nombre d'articles dans le panier
function updateCartCount() {
    const cart = JSON.parse(localStorage.getItem('cart')) || [];
    const productCount = cart.length;
    const cartItemCount = document.getElementById('cartItemCount');

    if (cartItemCount) {
        cartItemCount.textContent = productCount;
    }
}
function updateTotalAmount() {
    const cart = JSON.parse(localStorage.getItem('cart')) || [];
    let subtotal = 0;

    // Calculer le sous-total en fonction du panier
    cart.forEach(item => {
        subtotal += item.quantity * item.price;
    });

    const shipping = 10; // Vous pouvez ajuster cela si nécessaire
    const total = subtotal + shipping;

    // Mise à jour de l'affichage
    const subtotalElement = document.getElementById("subtotal");
    const shippingElement = document.getElementById("shipping");
    const totalElement = document.getElementById("total");
    const amountInput = document.getElementById("amount-input");

    if (!subtotalElement || !shippingElement || !totalElement || !amountInput) {
        console.error("Un des éléments du total est introuvable !");
        return;
    }

    subtotalElement.textContent = `$${subtotal.toFixed(2)}`;
    shippingElement.textContent = `$${shipping.toFixed(2)}`;
    totalElement.textContent = `$${total.toFixed(2)}`;
    amountInput.value = total.toFixed(2);

    localStorage.setItem('totalAmount', total.toFixed(2));

    console.log("Total mis à jour :", total.toFixed(2));
}

function initPaymentForm() {
    const paymentForm = document.getElementById("paymentForm");
    const totalElement = document.getElementById("total");
    const amountInput = document.getElementById("amount-input");

    if (!paymentForm || !totalElement || !amountInput) {
        console.error("Un élément du formulaire de paiement est introuvable !");
        return;
    }

    const storedTotalAmount = localStorage.getItem('totalAmount');
    console.log("Total récupéré depuis localStorage :", storedTotalAmount);

    if (storedTotalAmount) {
        const parsedAmount = parseFloat(storedTotalAmount);
        amountInput.value = parsedAmount.toFixed(2);
        totalElement.textContent = `$${parsedAmount.toFixed(2)}`;

        console.log("Valeur du champ amount-input mise à jour :", amountInput.value);  // Ajout d'un log pour vérifier
    }


    paymentForm.addEventListener("submit", function (event) {
        event.preventDefault();

        updateTotalAmount(); // Assure-toi que le total est bien mis à jour avant soumission

        const totalAmount = parseFloat(amountInput.value);
        console.log("Valeur du montant avant soumission :", totalAmount); // Ajoute ce log pour déboguer

        if (isNaN(totalAmount) || totalAmount <= 0) {
            alert("Montant invalide !");
            return;
        }

        document.getElementById("CartData").value = JSON.stringify(JSON.parse(localStorage.getItem('cart')) || []);
        paymentForm.submit();
    });

}
