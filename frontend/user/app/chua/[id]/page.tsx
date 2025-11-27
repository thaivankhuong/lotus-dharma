import { PAGODAS } from "@/app/chua/data";

interface Props {
  params: Promise<{ id: string }>; // NEXT 15 BẮT BUỘC PHẢI LÀ PROMISE
}

export default async function PagodaDetailPage({ params }: Props) {
  const { id } = await params;         // <-- GIẢI QUYẾT LỖI
    console.log(id);
  const pagoda = PAGODAS.find((p) => p.id === id);

  if (!pagoda) {
    return <div className="p-8 text-red-600">Không tìm thấy chùa!</div>;
  }

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-4">{pagoda.name}</h1>

      <p className="text-lg text-gray-700 mb-2">
        <strong>Địa điểm:</strong> {pagoda.location}
      </p>

      <p className="text-gray-600">{pagoda.description}</p>

      <a
        href="/chua"
        className="mt-6 inline-block px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
      >
        ← Quay lại danh sách
      </a>
      <button className="
  inline-flex items-center 
  px-4 py-2 
  border border-gray-300 
  text-gray-700 
  bg-white 
  rounded-lg 
  hover:bg-gray-50 
  focus:outline-none 
  focus:ring-2 
  focus:ring-blue-500 
  focus:ring-offset-2 
  transition
">
  ← Quay lại danh sách
</button>
    </div>
  );
}
