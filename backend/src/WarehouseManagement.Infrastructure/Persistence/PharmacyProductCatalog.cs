namespace WarehouseManagement.Infrastructure.Persistence;

/// <summary>
/// Danh mục ~500 thuốc / vật tư y tế thực tế dùng seed kho nhà thuốc bệnh viện.
/// Mỗi phần tử: tên hiển thị, chỉ số nhóm (map CategoryNames), giá tham chiếu (VND).
/// </summary>
public static class PharmacyProductCatalog
{
    public static IReadOnlyList<(string Name, int CategoryIndex, decimal Price)> All { get; } = Build();

    private static IReadOnlyList<(string Name, int CategoryIndex, decimal Price)> Build()
    {
        // (tên gốc, nhóm, giá cơ sở) — mở rộng bằng hàm lượng / quy cách để đủ 500 mã duy nhất.
        var cores = new (string Name, int Cat, decimal Price)[]
        {
            // Kháng sinh (0)
            ("Amoxicillin", 0, 45000), ("Amoxicillin/Acid clavulanic", 0, 85000), ("Ampicillin", 0, 38000),
            ("Azithromycin", 0, 72000), ("Clarithromycin", 0, 95000), ("Erythromycin", 0, 42000),
            ("Cefuroxime", 0, 110000), ("Cefixime", 0, 98000), ("Ceftriaxone", 0, 45000),
            ("Cefotaxime", 0, 52000), ("Ceftazidime", 0, 78000), ("Cefepime", 0, 125000),
            ("Ciprofloxacin", 0, 55000), ("Levofloxacin", 0, 88000), ("Ofloxacin", 0, 48000),
            ("Metronidazole", 0, 28000), ("Tinidazole", 0, 32000), ("Clindamycin", 0, 67000),
            ("Gentamicin", 0, 22000), ("Amikacin", 0, 35000), ("Vancomycin", 0, 185000),
            ("Meropenem", 0, 210000), ("Imipenem/Cilastatin", 0, 245000), ("Doxycycline", 0, 36000),
            ("Co-trimoxazole", 0, 25000), ("Nitrofurantoin", 0, 40000), ("Fosfomycin", 0, 92000),
            ("Linezolid", 0, 320000), ("Rifampicin", 0, 58000), ("Isoniazid", 0, 18000),
            // Giảm đau hạ sốt (1)
            ("Paracetamol", 1, 15000), ("Ibuprofen", 1, 22000), ("Aspirin", 1, 12000),
            ("Diclofenac", 1, 28000), ("Meloxicam", 1, 35000), ("Naproxen", 1, 32000),
            ("Celecoxib", 1, 78000), ("Tramadol", 1, 45000), ("Codein phosphat", 1, 38000),
            ("Morphin sulfat", 1, 65000), ("Pethidin", 1, 72000), ("Ketorolac", 1, 55000),
            ("Metamizol", 1, 26000), ("Etoricoxib", 1, 95000), ("Aceclofenac", 1, 42000),
            // Kháng viêm (2)
            ("Prednisolon", 2, 18000), ("Dexamethason", 2, 15000), ("Methylprednisolon", 2, 48000),
            ("Hydrocortison", 2, 35000), ("Betamethason", 2, 28000), ("Triamcinolon", 2, 40000),
            ("Budesonid", 2, 125000), ("Fluticason", 2, 145000), ("Mometason", 2, 98000),
            // Tim mạch (3)
            ("Amlodipin", 3, 32000), ("Nifedipin", 3, 28000), ("Diltiazem", 3, 45000),
            ("Losartan", 3, 55000), ("Valsartan", 3, 68000), ("Telmisartan", 3, 72000),
            ("Irbesartan", 3, 75000), ("Candesartan", 3, 82000), ("Perindopril", 3, 65000),
            ("Enalapril", 3, 28000), ("Lisinopril", 3, 35000), ("Ramipril", 3, 48000),
            ("Bisoprolol", 3, 42000), ("Metoprolol", 3, 38000), ("Atenolol", 3, 25000),
            ("Carvedilol", 3, 52000), ("Propranolol", 3, 22000), ("Atorvastatin", 3, 58000),
            ("Rosuvastatin", 3, 85000), ("Simvastatin", 3, 40000), ("Fenofibrat", 3, 45000),
            ("Clopidogrel", 3, 95000), ("Aspirin cardio", 3, 28000), ("Warfarin", 3, 32000),
            ("Rivaroxaban", 3, 185000), ("Dabigatran", 3, 210000), ("Digoxin", 3, 18000),
            ("Furosemid", 3, 12000), ("Hydrochlorothiazid", 3, 15000), ("Spironolacton", 3, 22000),
            ("Isosorbid mononitrat", 3, 35000), ("Nitroglycerin", 3, 28000), ("Amiodaron", 3, 65000),
            // Tiêu hóa (4)
            ("Omeprazol", 4, 35000), ("Pantoprazol", 4, 48000), ("Esomeprazol", 4, 72000),
            ("Lansoprazol", 4, 55000), ("Rabeprazol", 4, 62000), ("Famotidin", 4, 28000),
            ("Ranitidin", 4, 22000), ("Domperidon", 4, 18000), ("Metoclopramid", 4, 15000),
            ("Ondansetron", 4, 45000), ("Loperamid", 4, 12000), ("Diosmectit", 4, 38000),
            ("Lactulose", 4, 42000), ("Bisacodyl", 4, 16000), ("Simethicon", 4, 25000),
            ("Pancreatin", 4, 85000), ("Ursodeoxycholic acid", 4, 125000), ("Silymarin", 4, 55000),
            ("ORS điện giải", 4, 8000), ("Activated charcoal", 4, 18000), ("Hyoscin butylbromid", 4, 32000),
            // Hô hấp (5)
            ("Salbutamol", 5, 45000), ("Terbutalin", 5, 38000), ("Ipratropium", 5, 65000),
            ("Tiotropium", 5, 185000), ("Theophyllin", 5, 28000), ("Acetylcystein", 5, 35000),
            ("Bromhexin", 5, 18000), ("Ambroxol", 5, 22000), ("Dextromethorphan", 5, 20000),
            ("Montelukast", 5, 78000), ("Fexofenadin", 5, 55000), ("Pseudoephedrin", 5, 32000),
            // Thần kinh (6)
            ("Diazepam", 6, 18000), ("Alprazolam", 6, 35000), ("Lorazepam", 6, 42000),
            ("Clonazepam", 6, 48000), ("Phenobarbital", 6, 25000), ("Carbamazepin", 6, 38000),
            ("Valproat natri", 6, 55000), ("Levetiracetam", 6, 125000), ("Gabapentin", 6, 85000),
            ("Pregabalin", 6, 145000), ("Sertralin", 6, 78000), ("Fluoxetin", 6, 65000),
            ("Escitalopram", 6, 95000), ("Amitriptylin", 6, 28000), ("Quetiapin", 6, 110000),
            ("Risperidon", 6, 88000), ("Haloperidol", 6, 22000), ("Olanzapin", 6, 135000),
            ("Donepezil", 6, 165000), ("Memantin", 6, 185000), ("Sumatriptan", 6, 98000),
            // Nội tiết (7)
            ("Metformin", 7, 28000), ("Gliclazid", 7, 35000), ("Glimepirid", 7, 42000),
            ("Sitagliptin", 7, 185000), ("Empagliflozin", 7, 245000), ("Dapagliflozin", 7, 230000),
            ("Insulin human", 7, 125000), ("Insulin glargin", 7, 285000), ("Insulin aspart", 7, 265000),
            ("Levothyroxin", 7, 45000), ("Carbimazol", 7, 32000), ("Propylthiouracil", 7, 38000),
            ("Prednisolon nội tiết", 7, 22000), ("Desmopressin", 7, 195000), ("Octreotid", 7, 420000),
            // Vitamin khoáng (8)
            ("Vitamin C", 8, 25000), ("Vitamin B1", 8, 12000), ("Vitamin B6", 8, 12000),
            ("Vitamin B12", 8, 28000), ("Vitamin B1-B6-B12", 8, 35000), ("Acid folic", 8, 15000),
            ("Vitamin D3", 8, 45000), ("Vitamin A", 8, 18000), ("Vitamin E", 8, 32000),
            ("Calci carbonat", 8, 28000), ("Calci + D3", 8, 42000), ("Sắt fumarat", 8, 22000),
            ("Sắt + acid folic", 8, 35000), ("Kẽm sulfat", 8, 18000), ("Magie B6", 8, 38000),
            ("Multivitamin người lớn", 8, 55000), ("Multivitamin bà bầu", 8, 85000), ("Omega-3", 8, 95000),
            // Dịch truyền (9)
            ("Natri clorid 0.9%", 9, 12000), ("Glucose 5%", 9, 14000), ("Glucose 10%", 9, 16000),
            ("Ringer Lactate", 9, 18000), ("Ringer acetate", 9, 20000), ("Haes-steril", 9, 85000),
            ("Albumin 20%", 9, 450000), ("Mannitol 20%", 9, 35000), ("Natri bicarbonat 8.4%", 9, 28000),
            ("Kali clorid 10%", 9, 15000), ("Canxi clorid 10%", 9, 18000), ("Nước cất tiêm", 9, 5000),
            ("Plasma-Lyte", 9, 45000), ("Amino acid 8.5%", 9, 125000), ("Lipid emulsion 20%", 9, 185000),
            // Gây mê hồi sức (10)
            ("Propofol", 10, 85000), ("Ketamin", 10, 65000), ("Thiopental", 10, 55000),
            ("Midazolam", 10, 48000), ("Fentanyl", 10, 72000), ("Remifentanil", 10, 195000),
            ("Rocuronium", 10, 125000), ("Suxamethonium", 10, 45000), ("Atracurium", 10, 95000),
            ("Neostigmin", 10, 28000), ("Atropin sulfat", 10, 15000), ("Ephedrin", 10, 22000),
            ("Lidocain", 10, 18000), ("Bupivacain", 10, 35000), ("Sevofluran", 10, 285000),
            // Chống dị ứng (11)
            ("Cetirizin", 11, 22000), ("Loratadin", 11, 25000), ("Desloratadin", 11, 45000),
            ("Fexofenadin antihistamin", 11, 55000), ("Chlorpheniramin", 11, 12000), ("Diphenhydramin", 11, 15000),
            ("Promethazin", 11, 18000), ("Levocetirizin", 11, 38000), ("Bilastin", 11, 78000),
            // Da liễu (12)
            ("Clotrimazol kem", 12, 28000), ("Ketoconazol kem", 12, 35000), ("Mupirocin", 12, 65000),
            ("Fusidic acid kem", 12, 55000), ("Hydrocortison kem", 12, 25000), ("Betamethason kem", 12, 32000),
            ("Clobetasol kem", 12, 42000), ("Acyclovir kem", 12, 38000), ("Permethrin", 12, 45000),
            ("Benzoyl peroxid", 12, 48000), ("Tretinoin gel", 12, 55000), ("Silver sulfadiazin", 12, 65000),
            // Mắt TMH (13)
            ("Chloramphenicol nhỏ mắt", 13, 18000), ("Tobramycin nhỏ mắt", 13, 45000), ("Ofloxacin nhỏ mắt", 13, 38000),
            ("Dexamethason nhỏ mắt", 13, 28000), ("Artificial tears", 13, 35000), ("Timolol nhỏ mắt", 13, 55000),
            ("Latanoprost", 13, 185000), ("Xylometazolin mũi", 13, 22000), ("Oxymetazolin mũi", 13, 25000),
            ("Mometason xịt mũi", 13, 125000), ("NaCl 0.9% nhỏ mũi", 13, 15000), ("Ear drop ofloxacin", 13, 42000),
            // Sản phụ khoa (14)
            ("Oxytocin", 14, 28000), ("Misoprostol", 14, 45000), ("Methylergometrin", 14, 32000),
            ("Progesteron", 14, 85000), ("Noretisteron", 14, 38000), ("Combined oral contraceptive", 14, 65000),
            ("Clotrimazol đặt âm đạo", 14, 42000), ("Metronidazol đặt âm đạo", 14, 35000), ("Folic acid prenatal", 14, 28000),
            // Ung thư (15)
            ("Cyclophosphamid", 15, 125000), ("Doxorubicin", 15, 285000), ("Paclitaxel", 15, 450000),
            ("Cisplatin", 15, 195000), ("Carboplatin", 15, 320000), ("5-Fluorouracil", 15, 85000),
            ("Methotrexat", 15, 65000), ("Vincristin", 15, 175000), ("Tamoxifen", 15, 95000),
            ("Letrozol", 15, 185000), ("Imatinib", 15, 850000), ("Rituximab", 15, 1250000),
            // Thuốc tiêm (16)
            ("Adrenalin 1mg/ml", 16, 18000), ("Noradrenalin", 16, 45000), ("Dopamin", 16, 38000),
            ("Dobutamin", 16, 55000), ("Heparin 5000UI", 16, 42000), ("Enoxaparin", 16, 125000),
            ("Protamin sulfat", 16, 65000), ("Tranexamic acid tiêm", 16, 35000), ("Vitamin K1 tiêm", 16, 22000),
            ("Hydrocortison tiêm", 16, 28000), ("Methylprednisolon tiêm", 16, 48000), ("Omeprazol tiêm", 16, 55000),
            // Viên uống (17)
            ("Paracetamol viên", 17, 12000), ("Amoxicillin viên", 17, 35000), ("Omeprazol viên", 17, 28000),
            ("Losartan viên", 17, 45000), ("Metformin viên", 17, 22000), ("Atorvastatin viên", 17, 48000),
            ("Cetirizin viên", 17, 18000), ("Domperidon viên", 17, 15000), ("Bisoprolol viên", 17, 35000),
            ("Clopidogrel viên", 17, 78000), ("Amlodipin viên", 17, 25000), ("Gliclazid viên", 17, 28000),
            // Siro hỗn dịch (18)
            ("Paracetamol siro", 18, 28000), ("Ibuprofen siro", 18, 35000), ("Amoxicillin hỗn dịch", 18, 45000),
            ("Azithromycin hỗn dịch", 18, 85000), ("Cetirizin siro", 18, 32000), ("Salbutamol siro", 18, 38000),
            ("Lactulose siro", 18, 42000), ("Acetylcystein hỗn dịch", 18, 48000), ("Multivitamin siro trẻ em", 18, 55000),
            // Bôi ngoài da (19)
            ("Diclofenac gel", 19, 35000), ("Piroxicam gel", 19, 38000), ("Ketoprofen gel", 19, 42000),
            ("Methyl salicylate cream", 19, 28000), ("Calamin lotion", 19, 22000), ("Zinc oxide cream", 19, 18000),
            // Vắc xin (20)
            ("Vắc xin viêm gan B", 20, 185000), ("Vắc xin bạch hầu uốn ván", 20, 95000), ("Vắc xin cúm", 20, 245000),
            ("Vắc xin phế cầu", 20, 850000), ("Vắc xin HPV", 20, 1250000), ("Vắc xin zona", 20, 1850000),
            ("Vắc xin dại", 20, 320000), ("Vắc xin thủy đậu", 20, 450000), ("Immunoglobulin anti-HBs", 20, 650000),
            // Vật tư tiêu hao (21)
            ("Gạc vô trùng 10x10", 21, 8000), ("Gạc vô trùng 5x5", 21, 5000), ("Băng keo y tế", 21, 12000),
            ("Băng thun", 21, 15000), ("Khẩu trang y tế", 21, 25000), ("Khẩu trang N95", 21, 45000),
            ("Găng tay latex", 21, 85000), ("Găng tay nitrile", 21, 95000), ("Mũ trùm đầu", 21, 18000),
            ("Áo choàng phẫu thuật", 21, 45000), ("Ga trải giường vô trùng", 21, 65000), ("Túi đựng rác y tế", 21, 22000),
            ("Bông gòn y tế", 21, 15000), ("Cồn 70 độ", 21, 28000), ("Povidone iodine 10%", 21, 35000),
            ("Chlorhexidin 4%", 21, 42000), ("Hydrogen peroxid 3%", 21, 18000), ("Nước muối sinh lý rửa", 21, 12000),
            // Dụng cụ y tế (22)
            ("Ống tiêm 1ml", 22, 1500), ("Ống tiêm 3ml", 22, 2000), ("Ống tiêm 5ml", 22, 2500),
            ("Ống tiêm 10ml", 22, 3500), ("Ống tiêm 20ml", 22, 5000), ("Kim tiêm 18G", 22, 1200),
            ("Kim tiêm 21G", 22, 1000), ("Kim tiêm 23G", 22, 1000), ("Kim tiêm 25G", 22, 1200),
            ("Kim butterfly", 22, 4500), ("Catheter tĩnh mạch 18G", 22, 12000), ("Catheter tĩnh mạch 20G", 22, 12000),
            ("Catheter tĩnh mạch 22G", 22, 12000), ("Dây truyền dịch", 22, 8500), ("Dây truyền máu", 22, 15000),
            ("Kim luồn an toàn", 22, 18000), ("Ống thông tiểu Foley", 22, 35000), ("Ống hút đờm", 22, 8000),
            ("Masque oxy", 22, 25000), ("Canula oxy mũi", 22, 15000), ("Ambu bag người lớn", 22, 185000),
            ("Ambu bag nhi", 22, 165000), ("Ống nội khí quản", 22, 45000), ("Laryngoscope blade", 22, 125000),
            // Hóa chất XN (23)
            ("Que thử đường huyết", 23, 125000), ("Que thử nước tiểu 10 thông số", 23, 85000), ("Thuốc thử GOT/GPT", 23, 185000),
            ("Thuốc thử creatinin", 23, 165000), ("Thuốc thử cholesterol", 23, 145000), ("EDTA ống nghiệm", 23, 45000),
            ("Ống nghiệm heparin", 23, 48000), ("Ống nghiệm citrate", 23, 52000), ("Lam kính soi", 23, 25000),
            // Hướng thần (24)
            ("Diazepam hướng thần", 24, 22000), ("Alprazolam hướng thần", 24, 38000), ("Zolpidem", 24, 85000),
            ("Phenobarbital hướng thần", 24, 28000), ("Midazolam hướng thần", 24, 55000),
            // Gây nghiện (25)
            ("Morphin 10mg/ml", 25, 75000), ("Fentanyl 0.05mg/ml", 25, 85000), ("Pethidin 50mg/ml", 25, 68000),
            ("Codein 30mg", 25, 35000), ("Tramadol 100mg/2ml", 25, 42000),
            // Cấp cứu (26)
            ("Adrenalin cấp cứu", 26, 20000), ("Atropin cấp cứu", 26, 16000), ("Amiodaron cấp cứu", 26, 72000),
            ("Lidocain cấp cứu", 26, 22000), ("Natri bicarbonat cấp cứu", 26, 30000), ("Glucose 30% tiêm", 26, 18000),
            ("Calcium gluconate", 26, 25000), ("Naloxon", 26, 95000), ("Flumazenil", 26, 185000),
            // Dinh dưỡng LS (27)
            ("Ensure bột", 27, 285000), ("Glucerna", 27, 320000), ("Peptamen", 27, 385000),
            ("Sữa Albumin uống", 27, 245000), ("Amino acid uống", 27, 165000), ("TPN túi 2 ngăn", 27, 450000),
            // Kháng nấm / virus (28)
            ("Fluconazol", 28, 65000), ("Itraconazol", 28, 125000), ("Voriconazol", 28, 385000),
            ("Amphotericin B", 28, 285000), ("Nystatin", 28, 35000), ("Acyclovir uống", 28, 55000),
            ("Valacyclovir", 28, 185000), ("Oseltamivir", 28, 245000), ("Remdesivir", 28, 1850000),
            ("Lamivudin", 28, 95000), ("Tenofovir", 28, 125000), ("Efavirenz", 28, 145000),
            // Kháng đông cầm máu (29)
            ("Heparin kháng đông", 29, 45000), ("Enoxaparin kháng đông", 29, 135000), ("Warfarin kháng đông", 29, 35000),
            ("Rivaroxaban kháng đông", 29, 195000), ("Tranexamic cầm máu", 29, 38000), ("Vitamin K1 cầm máu", 29, 25000),
            ("Protamin cầm máu", 29, 72000), ("Etamsylat", 29, 28000), ("Desmopressin cầm máu", 29, 210000),
        };

        var strengths = new[]
        {
            "125mg", "250mg", "500mg", "650mg", "1g",
            "5mg", "10mg", "20mg", "40mg", "50mg", "100mg",
            "2ml", "5ml", "10ml", "100ml", "250ml", "500ml", "1000ml",
            "hộp 1 lọ", "hộp 10 ống", "hộp 2 vỉ x 10", "hộp 10 vỉ x 10", "túi 1 cái", "hộp 100 cái"
        };

        var list = new List<(string Name, int CategoryIndex, decimal Price)>(520);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, int cat, decimal price)
        {
            name = name.Trim();
            if (name.Length > 150) name = name[..150];
            if (!used.Add(name)) return;
            list.Add((name, cat, price));
        }

        foreach (var (name, cat, price) in cores)
            Add(name, cat, price);

        // Mở rộng đủ 500 tên duy nhất bằng quy cách / hàm lượng thực tế.
        var idx = 0;
        while (list.Count < 500 && idx < 100_000)
        {
            var core = cores[idx % cores.Length];
            var strength = strengths[idx % strengths.Length];
            var variant = ((idx / cores.Length) % 4) switch
            {
                0 => $"{core.Name} {strength}",
                1 => $"{core.Name} {strength} (nội viện)",
                2 => $"{core.Name} {strength} — DHG",
                _ => $"{core.Name} {strength} — Imexpharm"
            };
            var price = Math.Round(core.Price * (1.0m + (idx % 7) * 0.05m), 0);
            Add(variant, core.Cat, price);
            idx++;
        }

        return list.Take(500).ToList();
    }
}
