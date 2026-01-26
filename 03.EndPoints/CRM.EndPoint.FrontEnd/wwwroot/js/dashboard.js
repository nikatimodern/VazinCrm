
function renderTargetGaugeChart(value) {
    const COLORS = ["rgb(140, 214, 16)", "rgb(239, 198, 0)", "rgb(231, 24, 49)"];
    
    const index = (perc) => perc < 20 ? 2 : perc < 60 ? 1 : 0;
    
    const annotationConfig = {
        type: "doughnutLabel",
        content: ({ chart }) => [
            chart.data.datasets[0].data[0].toFixed(1) + " %",
            "پیشرفت فروش",
        ],
        drawTime: "beforeDraw",
        position: { y: "-50%" },
        font: [
            { size: 50, family: "IRANSansBold" },
            { size: 18, family: "IRANSans" },
        ],
        color: ({ chart }) => [COLORS[index(chart.data.datasets[0].data[0])], "grey"],
    };
    
    const data = {
        datasets: [{
            data: [value, 100 - value],
            backgroundColor(ctx) {
                if (ctx.type !== "data") return;
                return ctx.index === 1 ? "rgb(234, 234, 234)" : COLORS[index(ctx.raw)];
            },
        }],
    };
    
    const ctx = document.getElementById("gaugeChart").getContext("2d");
    
    new Chart(ctx, {
        type: "doughnut",
        data,
        options: {
            aspectRatio: 2,
            circumference: 180,
            rotation: -90,
            cutout: "70%",
            hoverOffset: 8,
            
            plugins: {
                tooltip: {
                    backgroundColor: "#333",
                    titleColor: "#fff",
                    bodyColor: "#eee",
                    titleFont: { size: 14, family: "IRANSansBold" },
                    bodyFont: { size: 13, family: "IRANSans" },
                },
                annotation: {
                    annotations: { annotationConfig },
                },
            },
        },
    });
}
// renderOrdersBarChart();

function renderOrdersBarChart() {
    // const labels = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"];
    const labels = ["فروردین", "اردیبهشت", "خرداد","شهریور", "مهر", "آبان"];
    
    const getRandomData = () => {
        const randomData = Array.from({ length: labels.length }, () =>
            Math.floor(Math.random() * 200),
        );
        return randomData;
    };
    
    const data = {
        labels: labels,
        datasets: [
            {
                label: "ثبت اولیه",
                data: getRandomData(),
                backgroundColor: "rgb(0 103 139)",
            },
            {
                label: "مرحله تولید",
                data: getRandomData(),
                backgroundColor: "rgb(137 139 0)",
            },
            {
                label: "آماده تحویل",
                data: getRandomData(),
                backgroundColor: "rgb(0 131 17)",
            },
            {
                label: "تحویل شده",
                data: getRandomData(),
                backgroundColor: "rgb(120 120 120)",
            },
        ],
    };
    
    const ctx = document.getElementById("ordersChart");
    new Chart(ctx, {
        type: "bar",
        data: data,
        options: {
            plugins: {
                title: {
                    display: false,
                    text: "Chart.js Bar Chart - Stacked",
                },
            },
            responsive: true,
            scales: {
                x: {
                    stacked: true,
                },
                y: {
                    stacked: true,
                },
            },
        },
    });
}

