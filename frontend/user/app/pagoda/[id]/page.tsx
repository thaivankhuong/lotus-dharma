async function getPagoda(id: string) {
  const res = await fetch(`http://localhost:3000/api/pagoda/${id}`, {
    cache: "no-store",
  });
  return res.json();
}

// Next.js 15: params là Promise -> await trước
export default async function PagodaDetailPage(
  props: { params: Promise<{ id: string }> }
) {
  const { id } = await props.params;

  const pagoda = await getPagoda(id);

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold">{pagoda.name}</h1>
      <p className="text-gray-600">Tỉnh: {pagoda.province}</p>

      <a
        href="/pagoda"
        className="inline-block mt-6 text-blue-500 underline"
      >
        ← Quay lại danh sách
      </a>
    </div>
  );
}
