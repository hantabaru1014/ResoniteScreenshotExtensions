// Proxying the API because direct fetch from api.resonite.com is not possible due to CORS

const userApiPrefix = "/api/users/";

export default {
  async fetch(request) {
    const url = new URL(request.url);

    if (url.pathname.startsWith(userApiPrefix)) {
      const userId = url.pathname.substring(userApiPrefix.length);
      return await fetch(`https://api.resonite.com/users/${userId}`);
    }

    return new Response(null, { status: 404 });
  },
} satisfies ExportedHandler<Env>;
