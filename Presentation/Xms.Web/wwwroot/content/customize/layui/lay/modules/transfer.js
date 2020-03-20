/** layui-v2.5.4 MIT License By https://www.layui.com */
; layui.define(["laytpl", "form"], function (e) { "use strict"; var a = layui.$, t = layui.laytpl, n = layui.form, i = "transfer", l = { config: {}, index: layui[i] ? layui[i].index + 1e4 : 0, set: function (e) { var t = this; return t.config = a.extend({}, t.config, e), t }, on: function (e, a) { return layui.onevent.call(this, i, e, a) } }, r = function () { var e = this, a = e.config, t = a.id || e.index; return r.that[t] = e, r.config[t] = a, { config: a, reload: function (a) { e.reload.call(e, a) }, getData: function () { return e.getData.call(e) } } }, c = "layui-hide", o = "layui-btn-disabled", d = "layui-none", s = "layui-transfer-box", u = "layui-transfer-header", h = "layui-transfer-search", f = "layui-transfer-active", y = "layui-transfer-data", p = function (e) { return e = e || {}, ['<div class="layui-transfer-box" data-index="' + e.index + '">', '<div class="layui-transfer-header">', '<input type="checkbox" name="' + e.checkAllName + '" lay-filter="layTransferCheckbox" lay-type="all" lay-skin="primary" title="{{ d.data.title[' + e.index + "] || 'list" + (e.index + 1) + "' }}\">", "</div>", "{{# if(d.data.showSearch){ }}", '<div class="layui-transfer-search">', '<i class="layui-icon layui-icon-search"></i>', '<input type="input" class="layui-input" placeholder="关键词搜索">', "</div>", "{{# } }}", '<ul class="layui-transfer-data"></ul>', "</div>"].join("") }, v = ['<div class="layui-transfer layui-form layui-border-box" lay-filter="LAY-transfer-{{ d.index }}">', p({ index: 0, checkAllName: "layTransferLeftCheckAll" }), '<div class="layui-transfer-active">', '<button type="button" class="layui-btn layui-btn-sm layui-btn-primary layui-btn-disabled" data-index="0">', '<i class="layui-icon layui-icon-next"></i>', "</button>", '<button type="button" class="layui-btn layui-btn-sm layui-btn-primary layui-btn-disabled" data-index="1">', '<i class="layui-icon layui-icon-prev"></i>', "</button>", "</div>", p({ index: 1, checkAllName: "layTransferRightCheckAll" }), "</div>"].join(""), x = function (e) { var t = this; t.index = ++l.index, t.config = a.extend({}, t.config, l.config, e), t.render() }; x.prototype.config = { title: ["列表一", "列表二"], width: 200, height: 360, data: [], value: [], showSearch: !1, id: "", text: { none: "无数据", searchNone: "无匹配数据" } }, x.prototype.reload = function (e) { var t = this; layui.each(e, function (e, a) { a.constructor === Array && delete t.config[e] }), t.config = a.extend(!0, {}, t.config, e), t.render() }, x.prototype.render = function () { var e = this, n = e.config, i = e.elem = a(t(v).render({ data: n, index: e.index })), l = n.elem = a(n.elem); l[0] && (n.data = n.data || [], n.value = n.value || [], e.key = n.id || e.index, l.html(e.elem), e.layBox = e.elem.find("." + s), e.layHeader = e.elem.find("." + u), e.laySearch = e.elem.find("." + h), e.layData = i.find("." + y), e.layBtn = i.find("." + f + " .layui-btn"), e.layBox.css({ width: n.width, height: n.height }), e.layData.css({ height: function () { return n.height - e.layHeader.outerHeight() - e.laySearch.outerHeight() - 2 }() }), e.renderData(), e.events()) }, x.prototype.renderData = function () { var e = this, a = (e.config, [{ checkName: "layTransferLeftCheck", views: [] }, { checkName: "layTransferRightCheck", views: [] }]); e.parseData(function (e) { var t = e.selected ? 1 : 0, n = ["<li>", '<input type="checkbox" name="' + a[t].checkName + '" lay-skin="primary" lay-filter="layTransferCheckbox" title="' + e.title + '"' + (e.disabled ? " disabled" : "") + (e.checked ? " checked" : "") + ' value="' + e.value + '">', "</li>"].join(""); a[t].views.push(n), delete e.selected }), e.layData.eq(0).html(a[0].views.join("")), e.layData.eq(1).html(a[1].views.join("")), e.renderCheckBtn() }, x.prototype.renderForm = function (e) { n.render(e, "LAY-transfer-" + this.index) }, x.prototype.renderCheckBtn = function (e) { var t = this, n = t.config; e = e || {}, t.layBox.each(function (i) { var l = a(this), r = l.find("." + y), d = l.find("." + u).find('input[type="checkbox"]'), s = r.find('input[type="checkbox"]'), h = 0, f = !1; if (s.each(function () { var e = a(this).data("hide"); (this.checked || this.disabled || e) && h++ , this.checked && !e && (f = !0) }), d.prop("checked", f && h === s.length), t.layBtn.eq(i)[f ? "removeClass" : "addClass"](o), !e.stopNone) { var p = r.children("li:not(." + c + ")").length; t.noneView(r, p ? "" : n.text.none) } }), t.renderForm("checkbox") }, x.prototype.noneView = function (e, t) { var n = a('<p class="layui-none">' + (t || "") + "</p>"); e.find("." + d)[0] && e.find("." + d).remove(), t.replace(/\s/g, "") && e.append(n) }, x.prototype.setValue = function () { var e = this, t = e.config, n = []; return e.layBox.eq(1).find("." + y + ' input[type="checkbox"]').each(function () { var e = a(this).data("hide"); e || n.push(this.value) }), t.value = n, e }, x.prototype.parseData = function (e) { var t = this, n = t.config, i = []; return layui.each(n.data, function (t, l) { l = ("function" == typeof n.parseData ? n.parseData(l) : l) || l, i.push(l = a.extend({}, l)), layui.each(n.value, function (e, a) { a == l.value && (l.selected = !0) }), e && e(l) }), n.data = i, t }, x.prototype.getData = function (e) { var a = this, t = a.config, n = []; return layui.each(e || t.value, function (e, a) { layui.each(t.data, function (e, t) { delete t.selected, a == t.value && n.push(t) }) }), n }, x.prototype.events = function () { var e = this, t = e.config; e.elem.on("click", 'input[lay-filter="layTransferCheckbox"]+', function () { var t = a(this).prev(), n = t[0].checked, i = t.parents("." + s).eq(0).find("." + y); t[0].disabled || ("all" === t.attr("lay-type") && i.find('input[type="checkbox"]').each(function () { this.disabled || (this.checked = n) }), e.renderCheckBtn({ stopNone: !0 })) }), e.layBtn.on("click", function () { var n = a(this), i = n.data("index"), l = e.layBox.eq(i), r = []; if (!n.hasClass(o)) { e.layBox.eq(i).each(function (t) { var n = a(this), i = n.find("." + y); i.children("li").each(function () { var t = a(this), n = t.find('input[type="checkbox"]'), i = n.data("hide"); n[0].checked && !i && (n[0].checked = !1, l.siblings("." + s).find("." + y).append(t.clone()), t.remove(), r.push(n[0].value)), e.setValue() }) }), e.renderCheckBtn(); var c = l.siblings("." + s).find("." + h + " input"); "" === c.val() || c.trigger("keyup"), t.onchange && t.onchange(e.getData(r), i) } }), e.laySearch.find("input").on("keyup", function () { var n = this.value, i = a(this).parents("." + h).eq(0).siblings("." + y), l = i.children("li"); l.each(function () { var e = a(this), t = e.find('input[type="checkbox"]'), i = t[0].title.indexOf(n) !== -1; e[i ? "removeClass" : "addClass"](c), t.data("hide", !i) }), e.renderCheckBtn(); var r = l.length === i.children("li." + c).length; e.noneView(i, r ? t.text.searchNone : "") }) }, r.that = {}, r.config = {}, l.reload = function (e, a) { var t = r.that[e]; return t.reload(a), r.call(t) }, l.getData = function (e) { var a = r.that[e]; return a.getData() }, l.render = function (e) { var a = new x(e); return r.call(a) }, e(i, l) });