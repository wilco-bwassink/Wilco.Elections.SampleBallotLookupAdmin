console.log("site.js loaded!");

document.addEventListener("DOMContentLoaded", () => {
  const electionIdInput =
    document.querySelector('input[name="ElectionId"]') ||
    document.querySelector('input[name="SelectedElectionId"]');
  const electionNameInput = document.querySelector('input[name="ElectionName"]');
  const selectedElectionIdInput = document.querySelector(
    'input[name="SelectedElectionId"]'
  );

  if (electionIdInput && electionNameInput && selectedElectionIdInput) {
    electionNameInput.addEventListener("input", () => {
      if (selectedElectionIdInput.value || electionIdInput.hasAttribute("readonly")) {
        return;
      }

      const suggestedId = electionNameInput.value
        .trim()
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "");

      electionIdInput.value = suggestedId;
    });
  }

  document.querySelectorAll(".delete-button").forEach((button) => {
    button.addEventListener("click", () => {
      const fileName = button.dataset.file;
      const electionId = electionIdInput ? electionIdInput.value : "";

      console.log("Sending AJAX delete for:", { electionId, fileName });
      console.log(
        "Encoded fetch body:",
        `electionId=${encodeURIComponent(
          electionId
        )}&fileName=${encodeURIComponent(fileName)}`
      );

      fetch("api/admin/delete-file", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: `electionId=${encodeURIComponent(
          electionId
        )}&fileName=${encodeURIComponent(fileName)}`,
      })
        .then((res) => res.json())
        .then((data) => {
          if (data.success) {
            const safeId = `file-${fileName
              .replace(/\s+/g, "-")
              .replace(/[./\\:"']/g, "")}`;
            const li = document.getElementById(safeId);
            if (li) li.remove();
            console.log(`✅ Deleted file: ${fileName}`);
          } else {
            alert("❌ Failed to delete file.");
          }
        })
        .catch(() => alert("❌ Error deleting file."));
    });
  });

  // JS version of .NET GetHashCode() for IDs
  function hashCode(str) {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      hash = (hash << 5) - hash + str.charCodeAt(i);
      hash |= 0;
    }
    return hash;
  }

  const deleteAllButton = document.getElementById("delete-all-sampleballots");
  if (deleteAllButton) {
    deleteAllButton.addEventListener("click", () => {
      const electionId = electionIdInput ? electionIdInput.value : "";

      if (!electionId) {
        alert("Election ID is missing.");
        return;
      }

      if (!confirm("Are you sure you want to delete all sample ballots?"))
        return;

      fetch("api/admin/delete-all-sampleballots", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: `electionId=${encodeURIComponent(electionId)}`,
      })
        .then((res) => res.json())
        .then((data) => {
          if (data.success) {
            const sampleBallotList = document.querySelector(
              "#sample-ballot-section ul"
            );
            if (sampleBallotList) {
              sampleBallotList.querySelectorAll("li").forEach((el) => {
                el.remove();
              });
            }
            console.log("✅ All sample ballot files deleted from UI");
          } else {
            alert("❌ Failed to delete sample ballots.");
          }
        })
        .catch(() => alert("❌ Error deleting sample ballots."));
    });
  }
});
