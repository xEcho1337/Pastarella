(function () {
  "use strict";

  function sendFeedback() {
    var subject = document.getElementById("feedbackSubject");
    var message = document.getElementById("feedbackMessage");
    var sendBtn = document.getElementById("sendFeedbackBtn");

    if (!message || !message.value || message.value === "") {
      alert("You have to insert a message");
      return;
    }

    if (sendBtn) {
      sendBtn.disabled = true;
      sendBtn.textContent = "Sending…";
    }

    fetch("https://pastarella.xreflection.workers.dev/feedback", {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        title: subject ? subject.value : "",
        body: message.value
      })
    }).then(function (res) {
      if (!res.ok) throw new Error("bad status");
      subject.value = "";
      message.value = "";
      var modalEl = document.getElementById("feedbackModal");
      if (modalEl && window.bootstrap) {
        var modal = window.bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
      }
    }).catch(function () {
      alert("An error occurred. Please retry");
    }).finally(function () {
      if (sendBtn) {
        sendBtn.disabled = false;
        sendBtn.textContent = "Send";
      }
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    var sendBtn = document.getElementById("sendFeedbackBtn");
    if (sendBtn) sendBtn.addEventListener("click", sendFeedback);
  });

  window.sendFeedback = sendFeedback;
})();
