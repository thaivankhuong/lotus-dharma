import { NextResponse } from "next/server";

const pagodas = [
  { id: 1, name: "Chùa One", province: "Đà Nẵng" },
  { id: 2, name: "Chùa Two", province: "Huế" },
  { id: 3, name: "Chùa Three", province: "Hà Nội" },
];

export async function GET(
  request: Request,
  context: { params: Promise<{ id: string }> }
) {
  const { id } = await context.params;  // 👈 BẮT BUỘC PHẢI AWAIT

  const pagoda = pagodas.find(x => x.id === Number(id));

  if (!pagoda) {
    return NextResponse.json({ error: "Not found" }, { status: 404 });
  }

  return NextResponse.json(pagoda);
}
