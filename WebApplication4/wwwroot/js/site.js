function sendForm() {

    let user = {

        name: document.getElementById("name").value,
        age: document.getElementById("age").value,
        email: document.getElementById("email").value

    };


    fetch("/Index?handler=Send",
        {
            method: "POST",

            headers:
            {
                "Content-Type": "application/json",

                "RequestVerificationToken":
                    document.querySelector(
                        'input[name="__RequestVerificationToken"]'
                    ).value
            },

            body: JSON.stringify(user)

        })

        .then(response => response.json())

        .then(data => {
            document.getElementById("result").innerHTML =
                `
        <h2>✅ ${data.message}</h2>

        <p>Имя: ${data.name}</p>
        <p>Возраст: ${data.age}</p>
        <p>Email: ${data.email}</p>
        `;
        })

        .catch(error => {
            document.getElementById("result").innerHTML =
                "Ошибка: " + error;
        });

}