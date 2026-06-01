let strings = {};
let lang = "hu";

async function init() {
  const res = await fetch("languages.json");
  strings = await res.json();

  const saved = localStorage.getItem("labirintus-lang");
  if (saved && strings[saved]) lang = saved;

  const select = document.getElementById("lang");
  select.innerHTML = "";
  for (const code of Object.keys(strings)) {
    const opt = document.createElement("option");
    opt.value = code;
    opt.textContent = strings[code].languageName;
    select.appendChild(opt);
  }
  select.value = lang;
  select.addEventListener("change", () => {
    lang = select.value;
    localStorage.setItem("labirintus-lang", lang);
    apply();
  });

  apply();
}

function t(key) {
  return strings[lang]?.[key] ?? strings.hu?.[key] ?? key;
}

function apply() {
  document.documentElement.lang = lang;
  document.title = t("pageTitle");

  document.querySelectorAll("[data-i18n]").forEach((el) => {
    const key = el.getAttribute("data-i18n");
    el.textContent = t(key);
  });

  document.querySelectorAll("[data-i18n-alt]").forEach((el) => {
    el.alt = t(el.getAttribute("data-i18n-alt"));
  });
}

init();
