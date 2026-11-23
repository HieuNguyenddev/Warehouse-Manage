/**
 * Sau khi login thành công trên Swagger → tự Authorize JWT.
 * Tránh lỗi 401 khi user chỉ Execute login rồi GET /products mà quên bấm ổ khóa.
 */
(function () {
  function applyBearer(token) {
    if (!token || !window.ui) return;
    try {
      window.ui.preauthorizeApiKey("Bearer", token);
    } catch (_) {
      /* ignore */
    }
    try {
      window.ui.authActions.authorize({
        Bearer: {
          name: "Bearer",
          schema: {
            type: "http",
            scheme: "bearer",
            bearerFormat: "JWT",
          },
          value: token,
        },
      });
    } catch (_) {
      /* ignore */
    }
    console.info(
      "[swagger] Đã tự Authorize JWT sau login. Có thể Execute GET/POST/PUT/DELETE."
    );
  }

  const originalFetch = window.fetch.bind(window);
  window.fetch = async function (input, init) {
    const response = await originalFetch(input, init);
    try {
      const url =
        typeof input === "string"
          ? input
          : input && input.url
            ? input.url
            : "";
      if (
        !url.includes("/api/v1/auth/admin/login") &&
        !url.includes("/api/v1/auth/customer/login")
      ) {
        return response;
      }
      if (!response.ok) return response;

      const clone = response.clone();
      const json = await clone.json();
      const token = json && json.data && json.data.token;
      if (typeof token === "string" && token.length > 0) {
        // Đợi Swagger UI sẵn sàng
        const tryApply = (left) => {
          if (window.ui) applyBearer(token);
          else if (left > 0) setTimeout(() => tryApply(left - 1), 200);
        };
        tryApply(25);
      }
    } catch (_) {
      /* ignore parse errors */
    }
    return response;
  };
})();
