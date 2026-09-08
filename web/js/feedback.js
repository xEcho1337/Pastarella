async function sendFeedback() {
    let subject = document.getElementById("feedbackSubject");
    let message = document.getElementById("feedbackMessage");

    if (!message.value || message.value === "") {
        alert("You have to insert a message");
        return;
    }

    try {
        const res = await fetch("https://pastarella.xreflection.workers.dev/feedback", {
            method: "POST",
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                title: subject.value,
                body: message.value,
            }),
        });

        if (!res.ok) {
            alert("An error occurred. Please retry");
            return;
        }
    } catch {
        alert("An error occurred. Please retry");
        return;
    }

    subject.value = "";
    message.value = "";
}