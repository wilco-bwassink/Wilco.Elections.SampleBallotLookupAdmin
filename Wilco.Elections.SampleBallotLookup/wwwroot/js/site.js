﻿console.log("site.js loaded!");

document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll(".delete-button").forEach((button) => {
    button.addEventListener("click", () => {
      const fileName = button.dataset.file;

      const electionInput =
        document.querySelector('input[name="ElectionName"]') ||
        document.querySelector('input[name="SelectedElection"]');
      const election = electionInput ? electionInput.value : "";

      console.log("Sending AJAX delete for:", { election, fileName });
      console.log(
        "Encoded fetch body:",
        `electionName=${encodeURIComponent(
          election
        )}&fileName=${encodeURIComponent(fileName)}`
      );

      fetch("api/admin/delete-fil", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: `electionName=${encodeURIComponent(
          election
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
      const electionInput =
        document.querySelector('input[name="ElectionName"]') ||
        document.querySelector('input[name="SelectedElection"]');
      const election = electionInput ? electionInput.value : "";

      if (!election) {
        alert("Election name is missing.");
        return;
      }

      if (!confirm("Are you sure you want to delete all sample ballots?"))
        return;

      fetch("api/admin/delete-all-sampleballots", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: `electionName=${encodeURIComponent(election)}`,
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
